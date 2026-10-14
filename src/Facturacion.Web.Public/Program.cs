using Facturacion.Configuration;
using Facturacion.Application.Interfaces;
using Facturacion.Infrastructure.Billing;
using Facturacion.Infrastructure.Email;
using Facturacion.Infrastructure.Identity;
using Facturacion.Infrastructure.Multitenancy;
using Facturacion.Infrastructure.Persistence;
using Facturacion.Infrastructure.Persistence.Providers;
using Facturacion.Web.Public.Components;
using Facturacion.Web.Public.Services;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddFacturacionSharedConfiguration(builder.Environment);

// Los ajustes locales son opcionales y nunca se versionan.
// Se cargan solo en Development; las variables de entorno y los argumentos
// siguen teniendo prioridad sobre este archivo.
if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddJsonFile("appsettings.Development.local.json", optional: true, reloadOnChange: true);
    builder.Configuration.AddEnvironmentVariables();
    builder.Configuration.AddCommandLine(args);
}
builder.Services.AddRazorComponents().AddInteractiveServerComponents();

// Identity solo para el registro: no depende de la configuración JWT de la API.
builder.Services.AddConfiguredDatabaseContext<AuthDbContext>(builder.Configuration);
builder.Services.AddIdentityCore<ApplicationUser>(options =>
{
    options.Password.RequiredLength = 12;
    options.Password.RequireDigit = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.User.RequireUniqueEmail = true;
}).AddEntityFrameworkStores<AuthDbContext>().AddDefaultTokenProviders();

// Contexto de tenant requerido por BillingDbContext y servicios de self-service
builder.Services.AddScoped<ITenantContext, Facturacion.Infrastructure.Multitenancy.TenantContext>();

// Servicios compartidos del backend, ejecutados exclusivamente en servidor.
builder.Services.AddSaasSelfService(builder.Configuration, includeWebhookProcessing: false);
builder.Services.AddFacturacionEmail(builder.Configuration);
builder.Services.AddScoped<PublicPlansService>();

// Base de datos. BillingDbContext y AuthDbContext comparten resolutor y cadenas.
builder.Services.AddConfiguredDatabaseContext<BillingDbContext>(
    builder.Configuration);


var app = builder.Build();
if (!app.Environment.IsDevelopment()) { app.UseExceptionHandler("/error"); app.UseHsts(); }
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
app.Run();
