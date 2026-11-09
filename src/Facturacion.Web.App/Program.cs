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
        ? "<p class='alert' role='alert'>Credenciales o tenant no válidos.</p>" : "";
    var html = $$"""
    <!doctype html>
    <html lang="es">
    <head>
      <meta charset="utf-8">
      <meta name="viewport" content="width=device-width,initial-scale=1">
      <meta name="color-scheme" content="light">
      <title>Iniciar sesión | FacturaciónES</title>
      <style>
        :root{font-family:Inter,ui-sans-serif,system-ui,-apple-system,"Segoe UI",sans-serif;color:#19243b;background:#f7f8fc}
        *{box-sizing:border-box}body{margin:0;min-height:100vh;min-height:100dvh}
        .page{display:grid;grid-template-columns:minmax(0,1fr) minmax(440px,1fr);min-height:100vh;min-height:100dvh}
        .showcase{position:relative;overflow:hidden;background:linear-gradient(145deg,#24246d 0%,#4539b5 55%,#6955dc 100%);color:#fff;padding:clamp(32px,5vw,76px);display:flex;flex-direction:column;justify-content:space-between}
        .showcase:before,.showcase:after{content:"";position:absolute;border-radius:50%;pointer-events:none}
        .showcase:before{width:540px;height:540px;right:-270px;top:8%;border:1px solid rgba(255,255,255,.18);box-shadow:0 0 0 90px rgba(255,255,255,.035),0 0 0 180px rgba(255,255,255,.025)}
        .showcase:after{width:300px;height:300px;left:-170px;bottom:-160px;background:rgba(255,255,255,.055)}
        .brand{position:relative;z-index:1;display:flex;align-items:center;gap:12px;font-size:1.2rem;font-weight:800;letter-spacing:-.04em}
        .mark{display:grid;place-items:center;width:42px;height:42px;border-radius:13px;background:#fff;color:#5145e7;font-weight:900;font-size:1.3rem}
        .pitch{position:relative;z-index:1;max-width:530px}.eyebrow{display:inline-flex;align-items:center;gap:9px;padding:9px 13px;border:1px solid rgba(255,255,255,.22);border-radius:999px;background:rgba(255,255,255,.1);font-size:.78rem;font-weight:650}
        .dot{width:7px;height:7px;border-radius:50%;background:#8cf0ca}
        .pitch h1{font-size:clamp(2.5rem,4vw,4.4rem);line-height:1.12;letter-spacing:-.06em;margin:26px 0 18px}
        .pitch p{font-size:1.08rem;line-height:1.8;color:#e2ddff;max-width:440px;margin:0}
        .foot{position:relative;z-index:1;font-size:.85rem;color:#d3cefb}
        .login-area{display:flex;align-items:center;justify-content:center;padding:48px 28px}
        .login{width:min(100%,420px)}.mobile-brand{display:none}
        .kicker{font-size:.8rem;font-weight:800;letter-spacing:.12em;text-transform:uppercase;color:#5145e7}
        .login h2{font-size:clamp(2rem,3vw,2.6rem);letter-spacing:-.05em;line-height:1.2;margin:12px 0 10px}
        .intro{color:#68758d;line-height:1.65;margin:0 0 34px}
        .field{margin-bottom:21px}.field label{display:block;font-size:.88rem;font-weight:700;margin-bottom:9px;color:#293650}
        .field input{display:block;width:100%;height:52px;padding:0 15px;border:1px solid #d8deeb;border-radius:12px;background:#fff;color:#19243b;outline:none;transition:border-color .2s,box-shadow .2s}
        .field input::placeholder{color:#9aa5b7}.field input:focus{border-color:#6656ed;box-shadow:0 0 0 4px rgba(102,86,237,.12)}
        .help{font-size:.78rem;color:#78849a;margin:8px 0 0;line-height:1.5}
        .submit{width:100%;min-height:54px;margin-top:10px;border:0;border-radius:12px;background:#5145e7;color:#fff;font-size:.98rem;font-weight:750;cursor:pointer;box-shadow:0 9px 22px rgba(81,69,231,.2);transition:background .2s,transform .2s}
        .submit:hover{background:#4036ce;transform:translateY(-1px)}.submit:focus-visible{outline:3px solid #9b93fa;outline-offset:3px}
        .alert{padding:13px 15px;margin:0 0 22px;border:1px solid #f1b7ba;border-radius:12px;background:#fff1f2;color:#a12a35;font-size:.9rem}
        .security{display:flex;align-items:center;gap:9px;margin-top:26px;font-size:.82rem;color:#78849a}
        .security svg{flex-shrink:0}.copyright{margin-top:44px;color:#9aa5b7;font-size:.78rem}
        @media(max-width:900px){.page{grid-template-columns:1fr}.showcase{display:none}.login-area{min-height:100vh;min-height:100dvh}.mobile-brand{display:flex;align-items:center;gap:10px;margin-bottom:54px;font-weight:800;color:#252354;letter-spacing:-.03em}.mobile-brand .mark{background:#5145e7;color:#fff;width:36px;height:36px;font-size:1rem}}
        @media(max-width:480px){.login-area{padding:32px 22px}.login h2{font-size:2rem}.intro{margin-bottom:26px}.copyright{margin-top:32px}}
        @media(prefers-reduced-motion:reduce){*,*:before,*:after{transition:none!important;scroll-behavior:auto!important}}
      </style>
    </head>
    <body>
      <main class="page">
        <aside class="showcase" aria-label="FacturaciónES">
          <div class="brand"><span class="mark" aria-hidden="true">F</span> FacturaciónES</div>
          <div class="pitch">
            <span class="eyebrow"><span class="dot" aria-hidden="true"></span> Tu espacio de gestión</span>
            <h1>Tu facturación, bajo control.</h1>
            <p>Gestiona tu actividad con una plataforma clara, segura y diseñada para simplificar tu día a día.</p>
          </div>
          <div class="foot">Facturación electrónica · España</div>
        </aside>
        <section class="login-area" aria-labelledby="login-title">
          <div class="login">
            <div class="mobile-brand"><span class="mark" aria-hidden="true">F</span> FacturaciónES</div>
            <div class="kicker">Bienvenido de nuevo</div>
            <h2 id="login-title">Inicia sesión</h2>
            <p class="intro">Accede a tu espacio de trabajo con tus credenciales.</p>
            {{error}}
            <form method="post" action="/login">
              <input type="hidden" name="__RequestVerificationToken" value="{{token}}">
              <div class="field"><label for="email">Correo electrónico</label><input id="email" type="email" name="email" placeholder="nombre@empresa.es" required autocomplete="username" autofocus></div>
              <div class="field"><label for="password">Contraseña</label><input id="password" type="password" name="password" placeholder="Introduce tu contraseña" required autocomplete="current-password"></div>
              <div class="field"><label for="tenantId">Identificador del tenant</label><input id="tenantId" name="tenantId" placeholder="xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx" required aria-describedby="tenant-help" spellcheck="false" autocomplete="off"><p class="help" id="tenant-help">Introduce el identificador de tu organización.</p></div>
              <button class="submit" type="submit">Entrar a mi cuenta &rarr;</button>
            </form>
            <div class="security"><svg width="17" height="17" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" aria-hidden="true"><rect x="5" y="10" width="14" height="11" rx="2"/><path d="M8 10V7a4 4 0 0 1 8 0v3"/></svg> Conexión protegida y acceso seguro</div>
            <div class="copyright">© FacturaciónES · Plataforma de gestión</div>
          </div>
        </section>
      </main>
    </body>
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
