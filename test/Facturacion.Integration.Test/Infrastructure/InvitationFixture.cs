using Facturacion.Application.Invitations;
using Facturacion.Application.Interfaces;
using Facturacion.Infrastructure.Billing;
using Facturacion.Infrastructure.Identity;
using Facturacion.Infrastructure.Invitations;
using Facturacion.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Facturacion.Integration.Test.Infrastructure;

/// <summary>
/// Contenedor minimo para ejercitar el flujo de invitacion con Identity,
/// AuthDbContext y BillingDbContext reales sobre SQLite.
/// </summary>
public sealed class InvitationFixture : IDisposable
{
    private readonly ServiceProvider _root;
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public static Guid TenantId { get; } = Guid.NewGuid();

    public InvitationFixture()
    {
        _connection.Open();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDataProtection();
        services.AddAuthentication();
        services.AddSingleton<IInvitationEmailSender, CapturingInvitationSender>();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Invitations:ExpirationHours"] = "48",
                ["Invitations:AcceptUrl"] = "https://app.example.test/aceptar-invitacion"
            })
            .Build());

        services.AddIdentityCore<ApplicationUser>(options =>
        {
            options.Password.RequiredLength = 12;
            options.Password.RequireDigit = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.User.RequireUniqueEmail = true;
        }).AddSignInManager().AddEntityFrameworkStores<AuthDbContext>().AddDefaultTokenProviders();

        services.AddSingleton<ITenantContext>(
            new BillingDatabaseFixture.StubTenantContext { TenantId = TenantId });
        services.AddDbContext<AuthDbContext>(o => o.UseSqlite(_connection));
        services.AddDbContext<BillingDbContext>(o => o.UseSqlite(_connection));
        services.AddScoped<IUserInvitationService, UserInvitationService>();

        _root = services.BuildServiceProvider();

        CreateSchema();
        Seed();
    }

    private void CreateSchema()
    {
        using var scope = _root.CreateScope();
        foreach (var ctx in new DbContext[]
                 {
                     scope.ServiceProvider.GetRequiredService<AuthDbContext>(),
                     scope.ServiceProvider.GetRequiredService<BillingDbContext>()
                 })
        {
            var script = ctx.Database.GenerateCreateScript();
            if (!string.IsNullOrWhiteSpace(script))
                ctx.Database.ExecuteSqlRaw(script);
        }
    }

    private void Seed()
    {
        using var scope = _root.CreateScope();
        var billing = scope.ServiceProvider.GetRequiredService<BillingDbContext>();
        var auth = scope.ServiceProvider.GetRequiredService<AuthDbContext>();

        billing.Tenants.Add(new Domain.Entities.Tenant { Id = TenantId, Name = "Tenant Invitaciones" });
        billing.SaveChanges();

        Company = new Domain.Entities.Company
        {
            TenantId = TenantId,
            Nif = $"B{Guid.NewGuid():N}"[..9],
            LegalName = "Empresa de invitaciones SL"
        };
        billing.Companys.Add(Company);
        billing.SaveChanges();

        Admin = NewUser("admin-invitaciones@example.test");
        auth.Users.Add(Admin);
        auth.SaveChanges();

        auth.TenantUsers.Add(new Domain.Entities.TenantUser
        { TenantId = TenantId, UserId = Admin.Id });
        auth.CompanyUsers.Add(new Domain.Entities.CompanyUser
        { TenantId = TenantId, CompanyId = Company.Id, UserId = Admin.Id, Role = "Admin" });
        auth.SaveChanges();
    }

    public ApplicationUser Admin { get; private set; } = null!;
    public Domain.Entities.Company Company { get; private set; } = null!;

    public static ApplicationUser NewUser(string email) => new()
    {
        Id = "user-" + Guid.NewGuid().ToString("N"),
        UserName = email,
        NormalizedUserName = email.ToUpperInvariant(),
        Email = email,
        NormalizedEmail = email.ToUpperInvariant(),
        EmailConfirmed = true,
        IsActive = true,
        SecurityStamp = Guid.NewGuid().ToString("N")
    };

    public IServiceScope CreateScope() => _root.CreateScope();

    public CapturingInvitationSender Emails =>
        (CapturingInvitationSender)_root.GetRequiredService<IInvitationEmailSender>();

    /// <summary>Ultimo token en claro enviado, tal y como lo recibiria el invitado.</summary>
    public string LastToken { get; private set; } = "";

    public string LastInviteUrl { get; private set; } = "";

    public void Dispose()
    {
        _root.Dispose();
        _connection.Dispose();
    }

    public sealed class CapturingInvitationSender : IInvitationEmailSender
    {
        public List<(string Email, string Url, DateTime Expires)> Sent { get; } = new();

        public Task SendInvitationAsync(
            string email, string invitationUrl, DateTime expiresAtUtc, CancellationToken ct)
        {
            Sent.Add((email, invitationUrl, expiresAtUtc));
            return Task.CompletedTask;
        }
    }
}