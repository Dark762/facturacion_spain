using Facturacion.Infrastructure.Identity;
using Facturacion.Integration.Test.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Facturacion.Integration.Test;

/// <summary>
/// Matriz de permisos por empresa. El servicio real es fail-closed: cualquier
/// rol o permiso desconocido se deniega.
/// </summary>
public sealed class CompanyAuthorizationTests : IClassFixture<BillingDatabaseFixture>
{
    private const string User = "usuario-de-prueba";
    private const string Foreign = "usuario-ajeno";

    private readonly BillingDatabaseFixture _fixture;

    public CompanyAuthorizationTests(BillingDatabaseFixture fixture) => _fixture = fixture;

    [Theory]
    [InlineData("Reader", "invoices.read", true)]
    [InlineData("Reader", "invoices.create", false)]
    [InlineData("Reader", "invoices.issue", false)]
    [InlineData("Editor", "invoices.read", true)]
    [InlineData("Editor", "invoices.create", true)]
    [InlineData("Editor", "invoices.issue", false)]
    [InlineData("Admin", "invoices.read", true)]
    [InlineData("Admin", "invoices.create", true)]
    [InlineData("Admin", "invoices.issue", true)]
    public async Task Role_permission_matrix_is_enforced(string role, string permission, bool allowed)
    {
        var seeded = _fixture.Seed();
        _fixture.SeedMembership(seeded, User, role);

        using var scope = _fixture.CreateScope(seeded.TenantId);
        BillingDatabaseFixture.SetUser(scope, User);

        var result = await scope.ServiceProvider
            .GetRequiredService<CompanyAuthorizationService>()
            .AuthorizeAsync(seeded.CompanyId, permission, CancellationToken.None);

        Assert.Equal(allowed, result.IsSuccess);
        if (!allowed)
            Assert.Equal("ACCESS_DENIED", result.Errors.Single().Code);
    }

    [Theory]
    [InlineData("")]
    [InlineData("invoices.delete")]
    [InlineData("invoices.")]
    [InlineData("INVOICES.READ")]
    [InlineData("admin")]
    public async Task Unknown_permission_is_denied_even_for_admin(string permission)
    {
        var seeded = _fixture.Seed();
        _fixture.SeedMembership(seeded, User, "Admin");

        using var scope = _fixture.CreateScope(seeded.TenantId);
        BillingDatabaseFixture.SetUser(scope, User);

        var result = await scope.ServiceProvider
            .GetRequiredService<CompanyAuthorizationService>()
            .AuthorizeAsync(seeded.CompanyId, permission, CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Unknown_role_is_denied()
    {
        var seeded = _fixture.Seed();
        _fixture.SeedMembership(seeded, User, "SuperUsuario");

        using var scope = _fixture.CreateScope(seeded.TenantId);
        BillingDatabaseFixture.SetUser(scope, User);

        var result = await scope.ServiceProvider
            .GetRequiredService<CompanyAuthorizationService>()
            .AuthorizeAsync(seeded.CompanyId, "invoices.read", CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Anonymous_caller_is_rejected_as_unauthorized()
    {
        var seeded = _fixture.Seed();

        using var scope = _fixture.CreateScope(seeded.TenantId);
        BillingDatabaseFixture.SetUser(scope, null);

        var result = await scope.ServiceProvider
            .GetRequiredService<CompanyAuthorizationService>()
            .AuthorizeAsync(seeded.CompanyId, "invoices.read", CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("UNAUTHORIZED", result.Errors.Single().Code);
    }

    [Fact]
    public async Task Member_of_another_tenant_is_denied()
    {
        var mine = _fixture.Seed();
        var theirs = _fixture.Seed();
        _fixture.SeedMembership(theirs, User, "Admin");

        using var scope = _fixture.CreateScope(mine.TenantId);
        BillingDatabaseFixture.SetUser(scope, User);

        var result = await scope.ServiceProvider
            .GetRequiredService<CompanyAuthorizationService>()
            .AuthorizeAsync(theirs.CompanyId, "invoices.issue", CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Tenant_member_without_company_role_is_denied()
    {
        var seeded = _fixture.Seed();
        _fixture.SeedTenantMembership(seeded, User);

        using var scope = _fixture.CreateScope(seeded.TenantId);
        BillingDatabaseFixture.SetUser(scope, User);

        var result = await scope.ServiceProvider
            .GetRequiredService<CompanyAuthorizationService>()
            .AuthorizeAsync(seeded.CompanyId, "invoices.read", CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Inactive_company_membership_is_denied()
    {
        var seeded = _fixture.Seed();
        var user = $"user-{Guid.NewGuid():N}"[..20];
        _fixture.SeedMembership(seeded, user, "Admin");

        using (var scope = _fixture.CreateScope(seeded.TenantId))
        {
            var auth = _fixture.GetAuth(scope);
            var membership = auth.CompanyUsers.Single(x => x.UserId == user);
            membership.IsActive = false;
            auth.SaveChanges();
        }

        using var check = _fixture.CreateScope(seeded.TenantId);
        BillingDatabaseFixture.SetUser(check, user);

        var result = await check.ServiceProvider
            .GetRequiredService<CompanyAuthorizationService>()
            .AuthorizeAsync(seeded.CompanyId, "invoices.issue", CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Inactive_tenant_membership_is_denied()
    {
        var seeded = _fixture.Seed();
        var user = $"user-{Guid.NewGuid():N}"[..20];
        _fixture.SeedMembership(seeded, user, "Admin");

        using (var scope = _fixture.CreateScope(seeded.TenantId))
        {
            var auth = _fixture.GetAuth(scope);
            auth.TenantUsers.Single(x => x.UserId == user).IsActive = false;
            auth.SaveChanges();
        }

        using var check = _fixture.CreateScope(seeded.TenantId);
        BillingDatabaseFixture.SetUser(check, user);

        var result = await check.ServiceProvider
            .GetRequiredService<CompanyAuthorizationService>()
            .AuthorizeAsync(seeded.CompanyId, "invoices.read", CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Inactive_company_is_denied()
    {
        var seeded = _fixture.Seed();
        _fixture.SeedMembership(seeded, User, "Admin");

        using (var scope = _fixture.CreateScope(seeded.TenantId))
        {
            var db = _fixture.GetDb(scope);
            db.Companys.Single(x => x.Id == seeded.CompanyId).IsActive = false;
            db.SaveChanges();
        }

        using var check = _fixture.CreateScope(seeded.TenantId);
        BillingDatabaseFixture.SetUser(check, User);

        var result = await check.ServiceProvider
            .GetRequiredService<CompanyAuthorizationService>()
            .AuthorizeAsync(seeded.CompanyId, "invoices.read", CancellationToken.None);

        Assert.True(result.IsFailure);
    }
}
