using Facturacion.Application.DTOs.Invoices;
using Facturacion.Application.Interfaces;
using Facturacion.Domain.Entities;
using Facturacion.Integration.Test.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Facturacion.Integration.Test;

/// <summary>
/// Cubre el calculo fiscal del borrador y, sobre todo, el indice unico de
/// (TenantId, CompanyId, Series, Number): los borradores se guardaban con
/// Series="" y Number="", de modo que el segundo borrador de una empresa
/// reventaba con DbUpdateException.
/// </summary>
public sealed class InvoiceDraftTests : IClassFixture<BillingDatabaseFixture>
{
    private readonly BillingDatabaseFixture _fixture;

    public InvoiceDraftTests(BillingDatabaseFixture fixture) => _fixture = fixture;

    private static CreateInvoiceRequest ValidRequest(
        BillingDatabaseFixture.SeededCompany seeded,
        string currency = "EUR",
        params (string Description, decimal Quantity, decimal UnitPrice, decimal VatRate)[] lines)
    {
        if (lines.Length == 0)
            lines = new[] { ("Consultoria", 2m, 50m, 21m) };

        return new CreateInvoiceRequest(
            seeded.CompanyId,
            seeded.CustomerId,
            new DateOnly(2026, 3, 15),
            currency,
            lines.Select(x => new CreateInvoiceLineRequest(
                x.Description, x.Quantity, x.UnitPrice, x.VatRate)).ToList());
    }

    [Fact]
    public async Task Draft_persists_lines_totals_and_tax_breakdown()
    {
        var seeded = _fixture.Seed();
        using var scope = _fixture.CreateScope(seeded.TenantId);

        var service = scope.ServiceProvider.GetRequiredService<IInvoiceApplicationService>();
        var request = ValidRequest(seeded, lines: new[]
        {
            ("Consultoria", 2m, 50m, 21m),
            ("Licencia anual", 1m, 120m, 10m)
        });

        var result = await service.CreateDraftAsync(request, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(220.00m, result.Value.TaxableBase);
        Assert.Equal(33.00m, result.Value.TaxAmount);
        Assert.Equal(253.00m, result.Value.Total);
        Assert.Equal("Draft", result.Value.Status);

        var db = _fixture.GetDb(scope);
        var stored = db.Invoices.Include(x => x.Lines).Include(x => x.TaxBreakdowns)
            .Single(x => x.Id == result.Value.Id);

        Assert.Equal(2, stored.Lines.Count);
        Assert.Equal(new[] { 1, 2 }, stored.Lines.Select(x => x.LineNumber).Order());
        Assert.Equal(2, stored.TaxBreakdowns.Count);
        Assert.Equal(
            new[] { 10m, 21m },
            stored.TaxBreakdowns.Select(x => x.TaxRate).Order());
    }

    [Fact]
    public async Task A_second_draft_for_the_same_company_does_not_collide()
    {
        var seeded = _fixture.Seed();
        using var scope = _fixture.CreateScope(seeded.TenantId);

        var service = scope.ServiceProvider.GetRequiredService<IInvoiceApplicationService>();

        // Regresion: antes fallaba en el segundo guardado por el indice unico.
        var first = await service.CreateDraftAsync(ValidRequest(seeded), CancellationToken.None);
        var second = await service.CreateDraftAsync(ValidRequest(seeded), CancellationToken.None);
        var third = await service.CreateDraftAsync(ValidRequest(seeded), CancellationToken.None);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.True(third.IsSuccess);
        Assert.Equal(3, _fixture.GetDb(scope).Invoices.Count());
    }

    [Fact]
    public async Task Draft_leaves_series_and_number_unassigned_until_issuance()
    {
        var seeded = _fixture.Seed();
        using var scope = _fixture.CreateScope(seeded.TenantId);

        var service = scope.ServiceProvider.GetRequiredService<IInvoiceApplicationService>();
        var result = await service.CreateDraftAsync(ValidRequest(seeded), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.Series);
        Assert.Null(result.Value.Number);
    }

    [Theory]
    [InlineData("USD")]
    [InlineData("GBP")]
    public async Task Only_eur_is_accepted(string currency)
    {
        var seeded = _fixture.Seed();
        using var scope = _fixture.CreateScope(seeded.TenantId);

        var service = scope.ServiceProvider.GetRequiredService<IInvoiceApplicationService>();
        var result = await service.CreateDraftAsync(
            ValidRequest(seeded, currency), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("CURRENCY_NOT_SUPPORTED", result.Errors.Single().Code);
    }

    [Fact]
    public async Task An_invoice_without_lines_is_rejected()
    {
        var seeded = _fixture.Seed();
        using var scope = _fixture.CreateScope(seeded.TenantId);

        var service = scope.ServiceProvider.GetRequiredService<IInvoiceApplicationService>();
        var request = ValidRequest(seeded) with { Lines = Array.Empty<CreateInvoiceLineRequest>() };

        var result = await service.CreateDraftAsync(request, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("INVOICE_LINES_REQUIRED", result.Errors.Single().Code);
    }

    [Theory]
    [InlineData(0, 50, 21)]
    [InlineData(-1, 50, 21)]
    [InlineData(2, 50, 7)]
    [InlineData(2, -50, 21)]
    public async Task Invalid_lines_are_rejected(decimal quantity, decimal price, decimal vat)
    {
        var seeded = _fixture.Seed();
        using var scope = _fixture.CreateScope(seeded.TenantId);

        var service = scope.ServiceProvider.GetRequiredService<IInvoiceApplicationService>();
        var request = ValidRequest(seeded, lines: new[] { ("Linea", quantity, price, vat) });

        var result = await service.CreateDraftAsync(request, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("INVALID_INVOICE_LINE", result.Errors.Single().Code);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(4, 4)]
    [InlineData(10, 10)]
    [InlineData(21, 21)]
    public async Task Vat_rates_supported_by_this_use_case_are_accepted(decimal vat, decimal expectedVatAmount)
    {
        var seeded = _fixture.Seed();
        using var scope = _fixture.CreateScope(seeded.TenantId);

        var service = scope.ServiceProvider.GetRequiredService<IInvoiceApplicationService>();
        var request = ValidRequest(seeded, lines: new[] { ("Base", 1m, 100m, vat) });

        var result = await service.CreateDraftAsync(request, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(expectedVatAmount, result.Value.TaxAmount);
    }

    [Fact]
    public async Task Draft_is_not_visible_from_another_tenant()
    {
        var seeded = _fixture.Seed();

        using (var writer = _fixture.CreateScope(seeded.TenantId))
        {
            var service = writer.ServiceProvider
                .GetRequiredService<IInvoiceApplicationService>();
            var created = await service.CreateDraftAsync(
                ValidRequest(seeded), CancellationToken.None);
            Assert.True(created.IsSuccess);
        }

        using var reader = _fixture.CreateScope(Guid.NewGuid());
        var other = _fixture.GetDb(reader).Invoices.AsNoTracking().ToList();

        Assert.Empty(other);
    }

    [Fact]
    public async Task Denied_authorization_short_circuits_before_any_write()
    {
        var seeded = _fixture.Seed();
        using var scope = _fixture.CreateScope(seeded.TenantId);

        var authorization = scope.ServiceProvider
            .GetRequiredService<BillingDatabaseFixture.StubCompanyAuthorizationService>();
        authorization.Allow = false;

        var service = scope.ServiceProvider.GetRequiredService<IInvoiceApplicationService>();
        var result = await service.CreateDraftAsync(
            ValidRequest(seeded), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("ACCESS_DENIED", result.Errors.Single().Code);
        Assert.Equal("invoices.create", authorization.LastPermission);
        Assert.Equal(seeded.CompanyId, authorization.LastCompanyId);
        Assert.Empty(_fixture.GetDb(scope).Invoices);
    }
}
