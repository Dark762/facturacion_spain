using Facturacion.Infrastructure.Billing;
using Facturacion.Infrastructure.Email;
using Facturacion.Infrastructure.Identity;
using Facturacion.Web.Public.Components;
using Facturacion.Web.Public.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorComponents().AddInteractiveServerComponents();

// La web pública ejecuta servicios en el servidor; no llama por HTTP a nuestra API.
builder.Services.AddSaasAuthentication(builder.Configuration);
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
