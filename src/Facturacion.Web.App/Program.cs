using Facturacion.Client;
using Facturacion.Configuration;
using Facturacion.Web.App.Components;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddFacturacionConfiguration(builder.Environment, FacturacionHost.WebApp);

// La web de aplicacion es una capa de presentacion: habla con la API por HTTP,
// no toca base de datos. No declara ninguna seccion obligatoria.
builder.Services.AddFacturacionOptions(ConfigurationScope.None);

builder.Services.AddRazorComponents();

// Configurable por host: en móvil/escritorio se utiliza el mismo registro con su URL.
var apiBaseUrl = builder.Configuration["Facturacion:ApiBaseUrl"];
if (!string.IsNullOrWhiteSpace(apiBaseUrl))
    builder.Services.AddFacturacionApiClient(new Uri(apiBaseUrl, UriKind.Absolute));

var app = builder.Build();
if (!app.Environment.IsDevelopment()) { app.UseExceptionHandler("/error"); app.UseHsts(); }
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();
app.MapRazorComponents<App>();
app.Run();