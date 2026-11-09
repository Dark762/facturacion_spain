using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Facturacion.Domain.Entities;
using Facturacion.Integration.Test.Infrastructure;
using Facturacion.Infrastructure.Identity;
using Facturacion.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Facturacion.Integration.Test;

/// <summary>
/// El login en dos fases depende de que JwtBearer reciba la clave de firma en el
/// esquema nombrado "Bearer". Si la configuracion se registra sin nombre, el
/// middleware se queda sin clave y responde 401 a tokens perfectamente validos.
/// </summary>
public sealed class AuthJwtConfigurationTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public AuthJwtConfigurationTests(ApiFactory factory) => _factory = factory;

    [Fact]
    public void Jwt_bearer_named_scheme_has_signing_key_issuer_and_events()
    {
        var options = _factory.Services
            .GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(JwtBearerDefaults.AuthenticationScheme);

        var parameters = options.TokenValidationParameters;

        var key = Assert.IsType<SymmetricSecurityKey>(parameters.IssuerSigningKey);
        Assert.Equal(ApiFactory.SigningKey, Encoding.UTF8.GetString(key.Key));
        Assert.True(parameters.ValidateIssuer);
        Assert.True(parameters.ValidateAudience);
        Assert.True(parameters.ValidateIssuerSigningKey);
        Assert.Equal("Facturacion.Api", parameters.ValidIssuer);
        Assert.Equal("Facturacion.Client", parameters.ValidAudience);
        Assert.False(options.MapInboundClaims);
        Assert.NotNull(options.Events?.OnTokenValidated);
        Assert.NotNull(options.Events?.OnAuthenticationFailed);
    }
}

/// <summary>
/// Regresion del flujo real: start devuelve un token de seleccion y complete lo
/// canjea por un JWT que debe servir contra endpoints de negocio.
/// </summary>
public sealed class AuthLoginFlowTests : IClassFixture<JwtApiFactory>
{
    private const string Password = "Integration!Pass123";

    private readonly JwtApiFactory _factory;

    public AuthLoginFlowTests(JwtApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Two_phase_login_issues_a_token_accepted_by_business_endpoints()
    {
        var email = $"flow-{Guid.NewGuid():N}@example.test";
        var tenantId = await SeedUserWithTenantAsync(email);

        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var start = await client.PostAsJsonAsync("/api/auth/login/start", new { email, password = Password });
        Assert.Equal(HttpStatusCode.OK, start.StatusCode);

        var selectionToken = await ReadDataPropertyAsync(start, "selectionToken");
        Assert.False(string.IsNullOrWhiteSpace(selectionToken));

        using var completeRequest = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login/complete")
        {
            Content = JsonContent.Create(new { tenantId, platform = false })
        };
        completeRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", selectionToken);

        var complete = await client.SendAsync(completeRequest);
        Assert.Equal(HttpStatusCode.OK, complete.StatusCode);

        var accessToken = await ReadDataPropertyAsync(complete, "accessToken");
        Assert.False(string.IsNullOrWhiteSpace(accessToken));

        using var businessRequest = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me/companies");
        businessRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var business = await client.SendAsync(businessRequest);
        Assert.Equal(HttpStatusCode.OK, business.StatusCode);
    }

    [Fact]
    public async Task Complete_rejects_a_selection_token_without_identity()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login/complete")
        {
            Content = JsonContent.Create(new { tenantId = Guid.NewGuid(), platform = false })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "no.es.un.jwt");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private async Task<Guid> SeedUserWithTenantAsync(string email)
    {
        using var scope = _factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var auth = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        var billing = scope.ServiceProvider.GetRequiredService<BillingDbContext>();

        var user = new ApplicationUser
        {
            Email = email,
            UserName = email,
            DisplayName = "Flujo de login",
            IsActive = true,
            EmailConfirmed = true
        };

        var created = await users.CreateAsync(user, Password);
        Assert.True(created.Succeeded,
            string.Join("; ", created.Errors.Select(error => error.Description)));

        var tenantId = Guid.NewGuid();

        auth.TenantUsers.Add(new TenantUser { TenantId = tenantId, UserId = user.Id });
        await auth.SaveChangesAsync();

        billing.Tenants.Add(new Tenant { Id = tenantId, Name = "Tenant de pruebas", IsActive = true });
        await billing.SaveChangesAsync();

        return tenantId;
    }

    private static async Task<string?> ReadDataPropertyAsync(HttpResponseMessage response, string property)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.GetProperty("data").GetProperty(property).GetString();
    }
}
