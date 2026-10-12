using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Facturacion.Integration.Test.Infrastructure;

public sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "Test";
    public const string HeaderName = "X-Test-User";
    public const string TenantHeaderName = "X-Test-Tenant";
    public const string DefaultUserId = "test-user-0001";

    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.ContainsKey(HeaderName))
            return Task.FromResult(AuthenticateResult.NoResult());

        var userId = Request.Headers[HeaderName].ToString();
        var tenantRaw = Request.Headers[TenantHeaderName].ToString();
        if (string.IsNullOrWhiteSpace(userId) || !Guid.TryParse(tenantRaw, out var tenantId))
            return Task.FromResult(AuthenticateResult.Fail("Cabeceras de prueba invalidas."));

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim("tenant_id", tenantId.ToString("D")),
            new Claim("security_stamp", "test-stamp")
        };

        var identity = new ClaimsIdentity(claims, SchemeName);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
