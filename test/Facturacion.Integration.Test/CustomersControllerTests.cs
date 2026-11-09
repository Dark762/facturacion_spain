using Facturacion.Api.Controllers;
using Facturacion.Application.Interfaces;
using Facturacion.Domain.Entities;
using Facturacion.Integration.Test.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Facturacion.Integration.Test;

/// <summary>Pruebas del controlador real con SQLite, filtros de tenant y autorización controlable.</summary>
public sealed class CustomersControllerTests : IClassFixture<BillingDatabaseFixture>
{
    private readonly BillingDatabaseFixture _fixture;
    public CustomersControllerTests(BillingDatabaseFixture fixture) => _fixture = fixture;

    private CustomersController Controller(IServiceScope scope)
    {
        var controller = new CustomersController(
            _fixture.GetDb(scope),
            scope.ServiceProvider.GetRequiredService<ICompanyAuthorizationService>(),
            scope.ServiceProvider.GetRequiredService<ITenantContext>());
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        return controller;
    }

    [Fact]
    public async Task List_searches_and_paginates_with_stable_order()
    {
        var seeded = _fixture.Seed();
        using var scope = _fixture.CreateScope(seeded.TenantId);
        var db = _fixture.GetDb(scope);
        db.Customers.AddRange(
            new Customer { TenantId = seeded.TenantId, CompanyId = seeded.CompanyId, TaxId = Guid.NewGuid().ToString("N"), LegalName = "Busqueda Alfa" },
            new Customer { TenantId = seeded.TenantId, CompanyId = seeded.CompanyId, TaxId = Guid.NewGuid().ToString("N"), LegalName = "Busqueda Beta" },
            new Customer { TenantId = seeded.TenantId, CompanyId = seeded.CompanyId, TaxId = Guid.NewGuid().ToString("N"), LegalName = "Busqueda Gamma" });
        await db.SaveChangesAsync();
        var controller = Controller(scope);
        var first = Assert.IsType<OkObjectResult>(await controller.List(seeded.CompanyId, CancellationToken.None, "Busqueda", 1, 2));
        var second = Assert.IsType<OkObjectResult>(await controller.List(seeded.CompanyId, CancellationToken.None, "Busqueda", 2, 2));
        Assert.Equal("3", controller.Response.Headers["X-Total-Count"].ToString());
        Assert.Equal("2", controller.Response.Headers["X-Page"].ToString());
        Assert.Equal("2", controller.Response.Headers["X-Page-Size"].ToString());
        var a = Extract(first);
        var b = Extract(second);
        Assert.Equal(new[] { "Busqueda Alfa", "Busqueda Beta" }, a.Select(x => x.LegalName));
        Assert.Equal("Busqueda Gamma", Assert.Single(b).LegalName);
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public async Task List_rejects_invalid_pagination(int page, int pageSize)
    {
        var seeded = _fixture.Seed();
        using var scope = _fixture.CreateScope(seeded.TenantId);
        Assert.Equal(400, Assert.IsType<ObjectResult>(await Controller(scope).List(
            seeded.CompanyId, CancellationToken.None, null, page, pageSize)).StatusCode);
    }

    [Fact]
    public async Task List_never_returns_customers_from_another_tenant()
    {
        var first = _fixture.Seed();
        var other = _fixture.Seed();
        using var scope = _fixture.CreateScope(first.TenantId);
        var result = Assert.IsType<OkObjectResult>(await Controller(scope).List(first.CompanyId, CancellationToken.None));
        Assert.Contains(Extract(result), x => x.Id == first.CustomerId);
        Assert.DoesNotContain(Extract(result), x => x.Id == other.CustomerId);
    }

    [Fact]
    public async Task Denied_permission_blocks_customer_list()
    {
        var seeded = _fixture.Seed();
        using var scope = _fixture.CreateScope(seeded.TenantId);
        scope.ServiceProvider.GetRequiredService<BillingDatabaseFixture.StubCompanyAuthorizationService>().Allow = false;
        Assert.Equal(403, Assert.IsType<ObjectResult>(await Controller(scope).List(seeded.CompanyId, CancellationToken.None)).StatusCode);
    }

    [Fact]
    public async Task Create_rejects_invalid_email_and_existing_tax_id()
    {
        var seeded = _fixture.Seed();
        using var scope = _fixture.CreateScope(seeded.TenantId);
        var controller = Controller(scope);
        var invalid = Request("NEW123", "bad@@example");
        Assert.Equal(400, Assert.IsType<ObjectResult>(await controller.Create(seeded.CompanyId, invalid, CancellationToken.None)).StatusCode);
        var existing = _fixture.GetDb(scope).Customers.Single(x => x.Id == seeded.CustomerId);
        existing.TaxId = "12345678Z";
        await _fixture.GetDb(scope).SaveChangesAsync();
        Assert.Equal(409, Assert.IsType<ObjectResult>(await controller.Create(seeded.CompanyId,
            Request(existing.TaxId, "valid@example.test"), CancellationToken.None)).StatusCode);
    }

    [Fact]
    public async Task Update_rejects_duplicate_tax_id_without_changing_customer()
    {
        var seeded = _fixture.Seed();
        using var scope = _fixture.CreateScope(seeded.TenantId);
        var db = _fixture.GetDb(scope);
        var other = new Customer { TenantId = seeded.TenantId, CompanyId = seeded.CompanyId,
            TaxId = Guid.NewGuid().ToString("N"), LegalName = "Otro cliente" };
        db.Customers.Add(other);
        await db.SaveChangesAsync();
        var existing = db.Customers.Single(x => x.Id == seeded.CustomerId);
        existing.TaxId = "12345678Z";
        await db.SaveChangesAsync();
        var existingTaxId = existing.TaxId;
        var result = await Controller(scope).Update(seeded.CompanyId, other.Id, Request(existingTaxId, "valid@example.test"), CancellationToken.None);
        Assert.Equal(409, Assert.IsType<ObjectResult>(result).StatusCode);
        Assert.Equal("Otro cliente", db.Customers.Single(x => x.Id == other.Id).LegalName);
    }

    [Fact]
    public async Task Delete_cannot_delete_customer_from_another_tenant()
    {
        var first = _fixture.Seed();
        var other = _fixture.Seed();
        using var scope = _fixture.CreateScope(first.TenantId);
        var result = await Controller(scope).Delete(first.CompanyId, other.CustomerId, CancellationToken.None);
        Assert.Equal(404, Assert.IsType<ObjectResult>(result).StatusCode);
    }

    [Theory]
    [InlineData("12345678Z", 201)]
    [InlineData("12345678A", 400)]
    [InlineData("X1234567L", 201)]
    [InlineData("X1234567A", 400)]
    [InlineData("B12345678", 400)]
    public async Task Create_validates_spanish_tax_identifiers(string taxId, int expectedStatus)
    {
        var seeded = _fixture.Seed();
        using var scope = _fixture.CreateScope(seeded.TenantId);
        var result = await Controller(scope).Create(seeded.CompanyId, Request(taxId, "valid@example.test"), CancellationToken.None);
        Assert.Equal(expectedStatus, Assert.IsAssignableFrom<ObjectResult>(result).StatusCode);
    }

    [Fact]
    public async Task Create_allows_foreign_tax_identifier_without_spanish_checksum()
    {
        var seeded = _fixture.Seed();
        using var scope = _fixture.CreateScope(seeded.TenantId);
        var request = Request("FR-ABC123", "valid@example.test") with { CountryCode = "FR" };
        var result = await Controller(scope).Create(seeded.CompanyId, request, CancellationToken.None);
        Assert.Equal(201, Assert.IsAssignableFrom<ObjectResult>(result).StatusCode);
    }

    private static CustomersController.SaveCustomerRequest Request(string taxId, string email) =>
        new(taxId, "Cliente de prueba", "Calle Mayor 1", "28001", "Madrid", "ES", email, true);

    private static IReadOnlyList<CustomersController.CustomerResponse> Extract(OkObjectResult result)
    {
        // La API usa ApiResponse<T>, que expone Data.
        var data = result.Value!.GetType().GetProperty("Data")!.GetValue(result.Value)!;
        return Assert.IsAssignableFrom<IReadOnlyList<CustomersController.CustomerResponse>>(data);
    }
}
