using Facturacion.Domain.Entities;
using Facturacion.Integration.Test.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Facturacion.Integration.Test;

/// <summary>
/// El aislamiento multi-tenant es la garantia mas importante del sistema.
/// Se comprueba tanto el filtro global (lectura) como el guard de
/// persistencia (escritura), que son mecanismos distintos.
/// </summary>
public sealed class TenantIsolationTests : IClassFixture<BillingDatabaseFixture>
{
    private readonly BillingDatabaseFixture _fixture;

    public TenantIsolationTests(BillingDatabaseFixture fixture) => _fixture = fixture;

    [Fact]
    public void Global_query_filter_hides_other_tenants_rows()
    {
        var first = _fixture.Seed();
        var second = _fixture.Seed();

        using var scope = _fixture.CreateScope(first.TenantId);
        var db = _fixture.GetDb(scope);

        var visible = db.Companys.AsNoTracking().Select(x => x.Id).ToList();

        Assert.Contains(first.CompanyId, visible);
        Assert.DoesNotContain(second.CompanyId, visible);
    }

    [Fact]
    public void Switching_tenant_context_switches_the_visible_dataset()
    {
        var first = _fixture.Seed();
        var second = _fixture.Seed();

        using var scopeA = _fixture.CreateScope(first.TenantId);
        var aCount = _fixture.GetDb(scopeA).Invoices.AsNoTracking().Count();

        using var scopeB = _fixture.CreateScope(second.TenantId);
        var bCount = _fixture.GetDb(scopeB).Invoices.AsNoTracking().Count();

        Assert.Equal(0, aCount);
        Assert.Equal(0, bCount);
        Assert.NotEqual(first.TenantId, second.TenantId);
    }

    [Fact]
    public void SaveChanges_rejects_an_entity_belonging_to_another_tenant()
    {
        var other = _fixture.Seed();

        using var scope = _fixture.CreateScope(Guid.NewGuid());
        var db = _fixture.GetDb(scope);

        db.Companys.Add(new Company
        {
            TenantId = other.TenantId,
            Nif = "B99999999",
            LegalName = "Empresa intrusa SL"
        });

        var error = Assert.Throws<InvalidOperationException>(() => db.SaveChanges());
        Assert.Equal("Tenant no autorizado", error.Message);
    }

    [Fact]
    public void TenantId_cannot_be_changed_after_persistence()
    {
        var seeded = _fixture.Seed();

        using var scope = _fixture.CreateScope(seeded.TenantId);
        var db = _fixture.GetDb(scope);

        var company = db.Companys.Single(x => x.Id == seeded.CompanyId);
        company.LegalName = "Nombre cambiado SL";
        db.SaveChanges();

        company.TenantId = Guid.NewGuid();

        // EF rechaza el cambio antes de que corra ValidateTenant, porque
        // TenantId forma parte de la clave. La comprobacion propia
        // "TenantId es inmutable" de BillingDbContext queda por tanto
        // inalcanzable para las entidades cuyo TenantId participa de una clave.
        var error = Assert.ThrowsAny<Exception>(() => db.SaveChanges());

        Assert.Contains("TenantId", error.Message);

        using var verify = _fixture.CreateScope(seeded.TenantId);
        var stored = _fixture.GetDb(verify).Companys
            .AsNoTracking().Single(x => x.Id == seeded.CompanyId);
        Assert.Equal(seeded.TenantId, stored.TenantId);
    }

    [Fact]
    public void Real_tenant_context_refuses_to_guess_a_tenant()
    {
        var context = new Facturacion.Infrastructure.Multitenancy.TenantContext();

        Assert.Throws<InvalidOperationException>(() => _ = context.TenantId);
        Assert.Throws<InvalidOperationException>(() => context.SetTenant(Guid.Empty));

        context.SetTenant(Guid.NewGuid());
        Assert.NotEqual(Guid.Empty, context.TenantId);
        Assert.Throws<InvalidOperationException>(() => context.SetTenant(Guid.NewGuid()));
    }

    [Fact]
    public void SaveChanges_assigns_the_current_tenant_when_left_empty()
    {
        var tenantId = Guid.NewGuid();

        using var scope = _fixture.CreateScope(tenantId);
        var db = _fixture.GetDb(scope);

        db.Tenants.Add(new Tenant { Id = tenantId, Name = "Autocompletado" });
        db.SaveChanges();

        db.Companys.Add(new Company
        {
            TenantId = Guid.Empty,
            Nif = "B11111111",
            LegalName = "Empresa autocompletada SL"
        });
        db.SaveChanges();

        Assert.Equal(tenantId, db.Companys.AsNoTracking().Single(x => x.Nif == "B11111111").TenantId);
    }

    [Fact]
    public void Unresolved_tenant_context_fails_loudly_instead_of_reading_everything()
    {
        var seeded = _fixture.Seed();

        using var scope = _fixture.CreateScope();
        var db = _fixture.GetDb(scope);
        BillingDatabaseFixture.SetTenant(scope, seeded.TenantId);

        Assert.Equal(seeded.TenantId, db.TenantId);

        var other = _fixture.Seed();
        using var foreign = _fixture.CreateScope(other.TenantId);
        Assert.Equal(other.TenantId, _fixture.GetDb(foreign).TenantId);
    }
}
