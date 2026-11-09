using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Antiforgery;
using System.Security.Claims;
using System.Net.Http.Json;
using System.Text.Json;
using Facturacion.Client;
using Facturacion.Configuration;
using Facturacion.Observability;
using Facturacion.Web.App.Components;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddFacturacionConfiguration(builder.Environment, FacturacionHost.WebApp);

builder.Services.AddFacturacionObservability(builder.Configuration, "Facturacion.Web.App", builder.Environment.EnvironmentName);

// La web de aplicacion es una capa de presentacion: habla con la API por HTTP,
// no toca base de datos. No declara ninguna seccion obligatoria.
builder.Services.AddFacturacionOptions(ConfigurationScope.None);

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "__Host-FacturacionSession";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.Path = "/";
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = false;
    });
builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient("FacturacionAuthApi", client =>
{
    var url = builder.Configuration["Facturacion:ApiBaseUrl"];
    if (string.IsNullOrWhiteSpace(url)) throw new InvalidOperationException("Facturacion:ApiBaseUrl es obligatorio para iniciar sesión.");
    client.BaseAddress = new Uri(url, UriKind.Absolute);
});
builder.Services.AddRazorComponents().AddInteractiveServerComponents();

// Configurable por host: en móvil/escritorio se utiliza el mismo registro con su URL.
var apiBaseUrl = builder.Configuration["Facturacion:ApiBaseUrl"];
if (!string.IsNullOrWhiteSpace(apiBaseUrl))
    builder.Services.AddFacturacionApiClient(new Uri(apiBaseUrl, UriKind.Absolute));

var app = builder.Build();
if (!app.Environment.IsDevelopment()) { app.UseExceptionHandler("/error"); app.UseHsts(); }
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();
// El formulario se entrega por el servidor con token antifalsificación.
app.MapGet("/login", (HttpContext http, IAntiforgery antiforgery) =>
{
    var token = antiforgery.GetAndStoreTokens(http).RequestToken;
    var error = http.Request.Query.ContainsKey("error")
        ? "<p role='alert'>Credenciales o tenant no válidos.</p>" : "";
    var html = $$"""
    <!doctype html><html lang="es"><head><meta charset="utf-8">
    <meta name="viewport" content="width=device-width,initial-scale=1">
    <title>Acceso | FacturaciónES</title></head>
    <body style="font:16px system-ui;max-width:420px;margin:10vh auto;padding:24px">
    <h1>Acceso a FacturaciónES</h1>{{error}}
    <form method="post" action="/login">
    <input type="hidden" name="__RequestVerificationToken" value="{{token}}">
    <p><label>Correo electrónico<br><input type="email" name="email" required autocomplete="username"></label></p>
    <p><label>Contraseña<br><input type="password" name="password" required autocomplete="current-password"></label></p>
    <p><label>Identificador de tenant<br><input name="tenantId" required placeholder="GUID del tenant"></label></p>
    <button type="submit">Entrar</button></form></body></html>
    """;
    return Results.Content(html, "text/html; charset=utf-8");
}).AllowAnonymous();

app.MapPost("/login", async (HttpContext http, IAntiforgery antiforgery,
    IHttpClientFactory clients, CancellationToken ct) =>
{
    try { await antiforgery.ValidateRequestAsync(http); }
    catch (AntiforgeryValidationException) { return Results.BadRequest(); }
    var form = await http.Request.ReadFormAsync(ct);
    var email = form["email"].ToString();
    var password = form["password"].ToString();
    if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password) ||
        !Guid.TryParse(form["tenantId"], out var tenantId))
        return Results.Redirect("/login?error=1");
    using var client = clients.CreateClient("FacturacionAuthApi");
    using var response = await client.PostAsJsonAsync("api/auth/login",
        new { email, password, tenantId }, ct);
    if (!response.IsSuccessStatusCode) return Results.Redirect("/login?error=1");
    var envelope = await response.Content.ReadFromJsonAsync<LoginEnvelope>(cancellationToken: ct);
    if (string.IsNullOrWhiteSpace(envelope?.Data?.AccessToken) ||
        envelope.Data.ExpiresUtc <= DateTime.UtcNow)
        return Results.Redirect("/login?error=1");
    var claims = new[]
    {
        new Claim(ClaimTypes.NameIdentifier, email),
        new Claim("api_token", envelope.Data.AccessToken),
        new Claim("tenant_id", tenantId.ToString("D"))
    };
    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    await http.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
        new ClaimsPrincipal(identity), new Microsoft.AspNetCore.Authentication.AuthenticationProperties
        {
            IsPersistent = false,
            ExpiresUtc = new DateTimeOffset(envelope.Data.ExpiresUtc, TimeSpan.Zero)
        });
    return Results.Redirect("/platform");
}).AllowAnonymous();

app.MapPost("/logout", async (HttpContext http, IAntiforgery antiforgery) =>
{
    try { await antiforgery.ValidateRequestAsync(http); }
    catch (AntiforgeryValidationException) { return Results.BadRequest(); }
    await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/login");
}).RequireAuthorization();

app.MapRazorComponents<App>().AddInteractiveServerRenderMode().RequireAuthorization();
app.Run();
internal sealed record LoginEnvelope(LoginPayload? Data);
internal sealed record LoginPayload(string AccessToken, DateTime ExpiresUtc);
