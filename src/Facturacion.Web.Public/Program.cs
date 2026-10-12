using Facturacion.Infrastructure.Billing;
using Facturacion.Infrastructure.Email;
using Facturacion.Infrastructure.Identity;
using Facturacion.Infrastructure.Persistence.Providers;
using Facturacion.Web.Public.Components;
using Facturacion.Web.Public.Services;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);
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

// Servicios compartidos del backend, ejecutados exclusivamente en servidor.
builder.Services.AddSaasSelfService(builder.Configuration);
builder.Services.AddFacturacionEmail(builder.Configuration);
builder.Services.AddScoped<PublicPlansService>();

var app = builder.Build();
if (!app.Environment.IsDevelopment()) { app.UseExceptionHandler("/error"); app.UseHsts(); }
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
app.Run();
