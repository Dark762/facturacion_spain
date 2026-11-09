using Facturacion.Application.Billing;
using Facturacion.Application.Provisioning;
using Facturacion.Configuration.Options;
using Facturacion.Infrastructure.Billing;
using Facturacion.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Facturacion.Integration.Test.Infrastructure;

/// <summary>
/// Contenedor minimo para ejercitar el alta self-service con Identity y
/// SelfServiceDbContext reales sobre SQLite.
/// </summary>
public sealed class RegistrationFixture : IDisposable
{
    private readonly ServiceProvider _root;
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public RegistrationFixture()
    {
        _connection.Open();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDataProtection();
        services.AddAuthentication();
        services.AddSingleton<IRegistrationEmailSender, CapturingEmailSender>();
        services.AddSingleton<IPaymentGateway, StubPaymentGateway>();
        services.AddSingleton<IProvisioningService, StubProvisioningService>();
        services.AddSingleton(Options.Create(new StripeOptions()));

        services.AddIdentityCore<ApplicationUser>(options =>
        {
            options.Password.RequiredLength = 12;
            options.Password.RequireDigit = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.User.RequireUniqueEmail = true;
        }).AddSignInManager().AddEntityFrameworkStores<AuthDbContext>().AddDefaultTokenProviders();

        services.AddDbContext<AuthDbContext>(o => o.UseSqlite(_connection));
        services.AddDbContext<SelfServiceDbContext>(o => o.UseSqlite(_connection));
        services.AddScoped<SaasRegistrationService>();

        _root = services.BuildServiceProvider();

        CreateSchema();
    }

    private void CreateSchema()
    {
        using var scope = _root.CreateScope();
        foreach (var context in new DbContext[]
                 {
                     scope.ServiceProvider.GetRequiredService<AuthDbContext>(),
                     scope.ServiceProvider.GetRequiredService<SelfServiceDbContext>()
                 })
        {
            var script = context.Database.GenerateCreateScript();
            if (!string.IsNullOrWhiteSpace(script))
                context.Database.ExecuteSqlRaw(script);
        }
    }

    public IServiceScope CreateScope() => _root.CreateScope();

    public T GetDb<T>(IServiceScope scope) where T : DbContext =>
        scope.ServiceProvider.GetRequiredService<T>();

    public CapturingEmailSender Emails =>
        (CapturingEmailSender)_root.GetRequiredService<IRegistrationEmailSender>();

    public void Dispose()
    {
        _root.Dispose();
        _connection.Dispose();
    }

    public sealed class CapturingEmailSender : IRegistrationEmailSender
    {
        public List<(string Email, Guid RegistrationId, string Token)> Sent { get; } = new();

        public Task SendConfirmationAsync(
            string email, Guid registrationId, string token, CancellationToken ct)
        {
            Sent.Add((email, registrationId, token));
            return Task.CompletedTask;
        }
    }

    /// <summary>Nunca debe llegar a Stripe en estas pruebas.</summary>
    public sealed class StubPaymentGateway : IPaymentGateway
    {
        public Task<(string SessionId, string Url)> CreateSubscriptionCheckoutAsync(
            string priceId, string customerEmail, string registrationId,
            string successUrl, string cancelUrl, CancellationToken ct) =>
            throw new NotSupportedException(
                "Las pruebas no deben invocar la pasarela de pago real.");
    }

    /// <summary>
    /// Los flujos de registro verificados aquí no alcanzan el aprovisionamiento.
    /// Si una prueba futura lo necesita, que lo haga explícito con un stub propio.
    /// </summary>
    public sealed class StubProvisioningService : IProvisioningService
    {
        private static NotSupportedException NotReached() => new(
            "El aprovisionamiento no forma parte de estas pruebas de registro.");

        public Task<ProvisioningResult> ProvisionAsync(ProvisioningRequest request, CancellationToken ct = default) =>
            Task.FromException<ProvisioningResult>(NotReached());

        public Task<BootstrapTenantResponse> BootstrapAsync(BootstrapTenantRequest request, CancellationToken ct) =>
            Task.FromException<BootstrapTenantResponse>(NotReached());

        public Task<CompanyResponse> CreateCompanyAsync(Guid tenantId, CreateCompanyRequest request, CancellationToken ct) =>
            Task.FromException<CompanyResponse>(NotReached());

        public Task<string> AddTenantUserAsync(Guid tenantId, CreateTenantUserRequest request, CancellationToken ct) =>
            Task.FromException<string>(NotReached());

        public Task AssignCompanyUserAsync(Guid tenantId, Guid companyId, AssignCompanyUserRequest request, CancellationToken ct) =>
            Task.FromException(NotReached());
    }
}