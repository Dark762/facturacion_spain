using Facturacion.Application.Common.Results;
using Facturacion.Application.Interfaces;
using Facturacion.Application.Services;
using Facturacion.Domain.Entities;
using Facturacion.Infrastructure.Identity;
using Facturacion.Infrastructure.Persistence;
using Facturacion.Infrastructure.Persistence.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Facturacion.Integration.Test.Infrastructure;

/// <summary>
/// Base de datos SQLite en memoria que replica el modelo real (claves
/// compuestas TenantId+Id, indices unicos y tipos longtext de MySQL).
/// Permite ejercitar de verdad el modelo, no una aproximacion en memoria.
/// </summary>
public sealed class BillingDatabaseFixture : IDisposable
{
    private readonly ServiceProvider _root;

    public SqliteConnection Connection { get; }

    public BillingDatabaseFixture()
    {
        Connection = new SqliteConnection("DataSource=:memory:");
        Connection.Open();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ITenantContext>(new StubTenantContext());
        services.AddScoped<ICurrentUserService, StubCurrentUserService>();

        // Sustituto controlable para los casos de uso, y el servicio real
        // registrado como tipo concreto para probar la matriz de permisos.
        services.AddScoped<StubCompanyAuthorizationService>();
        services.AddScoped<ICompanyAuthorizationService>(
            sp => sp.GetRequiredService<StubCompanyAuthorizationService>());
        services.AddScoped<CompanyAuthorizationService>();

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IInvoiceRepository, InvoiceRepository>();
        services.AddScoped<IInvoiceSequenceRepository, InvoiceSequenceRepository>();
        services.AddScoped<ICompanyRepository, CompanyRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IOutboxMessageRepository, OutboxMessageRepository>();
        services.AddScoped<IFiscalRecordRepository, FiscalRecordRepository>();
        services.AddScoped<IInvoiceApplicationService, InvoiceApplicationService>();
        services.AddScoped<IInvoiceIssuanceService, InvoiceIssuanceService>();

        services.AddDbContext<BillingDbContext>(o => o.UseSqlite(Connection));
        services.AddDbContext<AuthDbContext>(o => o.UseSqlite(Connection));

        _root = services.BuildServiceProvider();

        using var scope = _root.CreateScope();
        // EnsureCreated() solo crea el esquema del primer contexto: en cuanto
        // la base ya existe, el segundo devuelve false y omite sus tablas.
        EnsureSchema(scope.ServiceProvider.GetRequiredService<BillingDbContext>());
        EnsureSchema(scope.ServiceProvider.GetRequiredService<AuthDbContext>());
    }

    private static void EnsureSchema(DbContext context)
    {
        var script = context.Database.GenerateCreateScript();
        if (!string.IsNullOrWhiteSpace(script))
            context.Database.ExecuteSqlRaw(script);
    }

    public IServiceScope CreateScope(Guid? tenantId = null)
    {
        var scope = _root.CreateScope();
        if (tenantId.HasValue)
            SetTenant(scope, tenantId.Value);
        return scope;
    }

    public static void SetTenant(IServiceScope scope, Guid tenantId)
    {
        var context = (StubTenantContext)scope.ServiceProvider
            .GetRequiredService<ITenantContext>();
        context.TenantId = tenantId;
    }

    public static StubCurrentUserService SetUser(IServiceScope scope, string? userId)
    {
        var user = (StubCurrentUserService)scope.ServiceProvider
            .GetRequiredService<ICurrentUserService>();
        user.UserId = userId;
        user.IsAuthenticated = userId is not null;
        return user;
    }

    public BillingDbContext GetDb(IServiceScope scope) =>
        scope.ServiceProvider.GetRequiredService<BillingDbContext>();

    public AuthDbContext GetAuth(IServiceScope scope) =>
        scope.ServiceProvider.GetRequiredService<AuthDbContext>();

    /// <summary>Crea tenant, empresa y cliente. No crea membresias.</summary>
    public SeededCompany Seed(Guid? tenantId = null, string? nif = null)
    {
        var tenant = tenantId ?? Guid.NewGuid();
        using var scope = CreateScope(tenant);
        var db = GetDb(scope);

        db.Tenants.Add(new Tenant { Id = tenant, Name = $"Tenant {tenant}", IsActive = true });
        db.SaveChanges();

        var company = new Company
        {
            TenantId = tenant,
            Nif = nif ?? $"B{Guid.NewGuid():N}"[..9],
            LegalName = "Empresa de prueba SL",
            IsActive = true
        };
        db.Companys.Add(company);
        db.SaveChanges();

        var customer = new Customer
        {
            TenantId = tenant,
            CompanyId = company.Id,
            TaxId = $"B{Guid.NewGuid():N}"[..9],
            LegalName = "Cliente de prueba SL"
        };
        db.Customers.Add(customer);
        db.SaveChanges();

        return new SeededCompany(tenant, company.Id, customer.Id);
    }

    /// <summary>Aplica la pertenencia al tenant y el rol dentro de una empresa.</summary>
    public void SeedMembership(SeededCompany seeded, string userId, string role)
    {
        using var scope = CreateScope(seeded.TenantId);
        var auth = GetAuth(scope);

        EnsureIdentityUser(auth, userId);

        auth.TenantUsers.Add(new TenantUser
        {
            TenantId = seeded.TenantId,
            UserId = userId,
            IsActive = true
        });
        auth.CompanyUsers.Add(new CompanyUser
        {
            TenantId = seeded.TenantId,
            CompanyId = seeded.CompanyId,
            UserId = userId,
            Role = role,
            IsActive = true
        });
        auth.SaveChanges();
    }

    public void SeedTenantMembership(SeededCompany seeded, string userId)
    {
        using var scope = CreateScope(seeded.TenantId);
        var auth = GetAuth(scope);

        EnsureIdentityUser(auth, userId);

        auth.TenantUsers.Add(new TenantUser
        {
            TenantId = seeded.TenantId,
            UserId = userId,
            IsActive = true
        });
        auth.SaveChanges();
    }

    /// <summary>
    /// TenantUser y CompanyUser declaran FK contra AspNetUsers, asi que la
    /// identidad debe existir antes de sembrar las membresias.
    /// </summary>
    private static void EnsureIdentityUser(AuthDbContext auth, string userId)
    {
        if (auth.Users.Any(u => u.Id == userId)) return;

        auth.Users.Add(new ApplicationUser
        {
            Id = userId,
            UserName = userId,
            NormalizedUserName = userId.ToUpperInvariant(),
            Email = $"{userId}@example.test",
            NormalizedEmail = $"{userId}@example.test".ToUpperInvariant(),
            SecurityStamp = Guid.NewGuid().ToString("N"),
            DisplayName = userId,
            IsActive = true
        });
        auth.SaveChanges();
    }

    public void Dispose()
    {
        _root.Dispose();
        Connection.Dispose();
    }

    public sealed record SeededCompany(Guid TenantId, Guid CompanyId, Guid CustomerId);

    public sealed class StubTenantContext : ITenantContext
    {
        public Guid TenantId { get; set; } = Guid.Parse("11111111-1111-1111-1111-111111111111");
    }

    public sealed class StubCurrentUserService : ICurrentUserService
    {
        public string? UserId { get; set; }
        public bool IsAuthenticated { get; set; } = true;
    }

    public sealed class StubCompanyAuthorizationService : ICompanyAuthorizationService
    {
        public bool Allow { get; set; } = true;
        public string? LastPermission { get; private set; }
        public Guid LastCompanyId { get; private set; }

        public Task<Result> AuthorizeAsync(Guid companyId, string permission, CancellationToken ct)
        {
            LastCompanyId = companyId;
            LastPermission = permission;
            return Task.FromResult(Allow
                ? Result.Success()
                : Result.Failure(new Error(
                    "ACCESS_DENIED", "Denegado en pruebas.", ErrorType.Forbidden)));
        }
    }
}
