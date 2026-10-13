using Facturacion.Configuration;
using Facturacion.Web.App.Components;
var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddFacturacionSharedConfiguration(builder.Environment);
builder.Services.AddRazorComponents();
var app = builder.Build();
if (!app.Environment.IsDevelopment()) { app.UseExceptionHandler("/error"); app.UseHsts(); }
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();
app.MapRazorComponents<App>();
app.Run();
