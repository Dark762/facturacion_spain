using System.Net;
using System.Net.Http.Headers;
using Facturacion.Integration.Test.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Facturacion.Integration.Test;

/// <summary>
/// El arranque y el pipeline son la primera linea de defensa: si aqui se
/// rompe, todo lo demas da igual.
/// </summary>
public sealed class ApiPipelineTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    private readonly HttpClient _client;

    public ApiPipelineTests(ApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public void Application_starts_and_resolves_its_dependency_graph()
    {
        // Obliga al host a construir el contenedor completo.
        using var scope = _factory.Services.CreateScope();
        Assert.NotNull(scope.ServiceProvider.GetService<
            Facturacion.Application.Interfaces.IInvoiceApplicationService>());
        Assert.NotNull(scope.ServiceProvider.GetService<
            Facturacion.Infrastructure.Persistence.Providers.DatabaseProviderResolver>());
        Assert.NotNull(scope.ServiceProvider.GetService<
            Facturacion.Infrastructure.Identity.AuthDbContext>());
    }

    [Fact]
    public async Task Protected_endpoint_rejects_anonymous_call()
    {
        var response = await _client.GetAsync("/api/auth/me/companies");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Protected_endpoint_rejects_a_malformed_token()
    {
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "no.es.un.jwt");

        var response = await _client.GetAsync("/api/auth/me/companies");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Protected_endpoint_rejects_a_token_signed_with_another_key()
    {
        var forged = CreateToken("clave-falsa-de-al-menos-32-bytes-para-firmar!!");
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", forged);

        var response = await _client.GetAsync("/api/auth/me/companies");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Preflight_from_configured_origin_is_allowed()
    {
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/auth/login");
        request.Headers.Add("Origin", ApiFactory.AllowedOrigin);
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add("Access-Control-Request-Headers", "content-type");

        var response = await _client.SendAsync(request);

        Assert.Equal(
            ApiFactory.AllowedOrigin,
            response.Headers.GetValues("Access-Control-Allow-Origin").Single());
    }

    [Fact]
    public async Task Preflight_from_unknown_origin_is_not_allowed()
    {
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/auth/login");
        request.Headers.Add("Origin", "https://evil.example");
        request.Headers.Add("Access-Control-Request-Method", "POST");

        var response = await _client.SendAsync(request);

        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task Login_rejects_a_malformed_payload_with_400_not_500()
    {
        var content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/api/auth/login", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static string CreateToken(string signingKey)
    {
        var key = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
            System.Text.Encoding.UTF8.GetBytes(signingKey));
        var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
            issuer: "Facturacion.Api",
            audience: "Facturacion.Client",
            claims: new[]
            {
                new System.Security.Claims.Claim(
                    System.Security.Claims.ClaimTypes.NameIdentifier, "forged-user"),
                new System.Security.Claims.Claim("tenant_id", Guid.NewGuid().ToString("D")),
                new System.Security.Claims.Claim("security_stamp", "forged")
            },
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: new Microsoft.IdentityModel.Tokens.SigningCredentials(
                key, Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256));

        return new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token);
    }
}
