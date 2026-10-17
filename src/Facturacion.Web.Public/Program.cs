using System.Text;
using System.Text.Json;
using Facturacion.Payments.Stripe;
using Facturacion.Application.Interfaces;
using Facturacion.Configuration;
using Facturacion.Configuration.Options;
using Facturacion.Infrastructure.Billing;
using Facturacion.Infrastructure.Email;
using Facturacion.Infrastructure.Identity;
using Facturacion.Infrastructure.Multitenancy;
using Facturacion.Infrastructure.Persistence;
using Facturacion.Infrastructure.Persistence.Providers;
using Facturacion.Web.Public.Components;
using Facturacion.Web.Public.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddFacturacionConfiguration(builder.Environment, FacturacionHost.WebPublic);

// Los ajustes locales son opcionales y nunca se versionan.
// Se cargan solo en Development; las variables de entorno y los argumentos
// siguen teniendo prioridad sobre este archivo.
if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddJsonFile("appsettings.Development.local.json", optional: true, reloadOnChange: true);
    builder.Configuration.AddEnvironmentVariables();
    builder.Configuration.AddCommandLine(args);
}

// Sin esto no hay ninguna clave tipada: se valida al arrancar y avisa de
// ConnectionStrings:Default si falta.
builder.Services.AddFacturacionOptions(ConfigurationScope.Database);

// AddRazorComponents aporta tambien Antiforgery, que UseAntiforgery exige. No es
// un detalle: sin este registro el arranque falla al llegar al middleware.
builder.Services.AddRazorComponents().AddInteractiveServerComponents();

// AuthDbContext y BillingDbContext comparten resolutor y cadenas.
builder.Services.AddConfiguredDatabaseContext<AuthDbContext>();
builder.Services.AddConfiguredDatabaseContext<BillingDbContext>();

// Identity solo para el registro: no depende de la configuracion JWT de la API,
// pero comparte la politica de contrasenas para que un usuario no pueda
// registrar aqui una contrasena que la API le rechazaria.
//
// AddDataProtection no es opcional: lo exige DataProtectorTokenProvider, que es
// lo que AddDefaultTokenProviders registra para emitir los tokens de
// verificacion de correo. AddIdentityCore no lo trae; solo lo trae
// AddAuthentication, que aqui no se llama porque no hay esquema de sesion.
builder.Services.AddDataProtection();
builder.Services.AddSingleton<IConfigureOptions<IdentityOptions>, ConfigurePublicIdentityPolicy>();
builder.Services.AddIdentityCore<ApplicationUser>()
    .AddEntityFrameworkStores<AuthDbContext>()
    .AddDefaultTokenProviders();

// Contexto de tenant requerido por BillingDbContext y por el registro.
// Se registra la clase y la interfaz porque el aprovisionamiento necesita
// poder fijar el tenant, no solo consultarlo.
builder.Services.AddScoped<TenantContext>();
builder.Services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<TenantContext>());

// Alta self-service + Stripe. La web publica recibe temporalmente el webhook
// mientras la API no se ejecuta; reutiliza el procesador compartido.
builder.Services.AddSaasSelfService(includeWebhookProcessing: true);
builder.Services.AddFacturacionEmail();
builder.Services.AddScoped<PublicPlansService>();

var app = builder.Build();
if (!app.Environment.IsDevelopment()) { app.UseExceptionHandler("/error"); app.UseHsts(); }
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();
// Endpoint de Stripe en el host publico, sin depender de Facturacion.Api.
// No se debe activar la suscripcion desde la URL de exito de Checkout.
app.MapPost("/api/webhooks/stripe", async (
    HttpRequest request,
    StripeEventProcessor processor,
    IOptions<StripeOptions> stripeOptions,
    CancellationToken ct) =>
{
    using var buffer = new MemoryStream();
    await request.Body.CopyToAsync(buffer, ct);
    var payload = buffer.ToArray();

    if (!StripeWebhookVerifier.Verify(
            payload,
            request.Headers["Stripe-Signature"].ToString(),
            stripeOptions.Value.WebhookSecret))
        return Results.BadRequest();

    JsonDocument document;
    try { document = JsonDocument.Parse(payload); }
    catch (JsonException) { return Results.BadRequest(); }

    using (document)
    {
        var outcome = await processor.HandleAsync(document.RootElement, ct);
        return outcome == StripeEventOutcome.RetryLater
            ? Results.StatusCode(StatusCodes.Status503ServiceUnavailable)
            : Results.Ok();
    }
}).AllowAnonymous().DisableAntiforgery();

app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
app.Run();

/// <summary>
/// Misma politica de contrasenas que la API, leida del contrato compartido.
/// </summary>
internal sealed class ConfigurePublicIdentityPolicy(
    IOptions<IdentityPolicyOptions> policy) : IConfigureOptions<IdentityOptions>
{
    public void Configure(IdentityOptions options)
    {
        var value = policy.Value;

        options.Password.RequiredLength = value.RequiredLength;
        options.Password.RequireDigit = value.RequireDigit;
        options.Password.RequireUppercase = value.RequireUppercase;
        options.Password.RequireNonAlphanumeric = value.RequireNonAlphanumeric;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(value.LockoutMinutes);
        options.Lockout.MaxFailedAccessAttempts = value.MaxFailedAccessAttempts;
        options.User.RequireUniqueEmail = value.RequireUniqueEmail;
    }
}