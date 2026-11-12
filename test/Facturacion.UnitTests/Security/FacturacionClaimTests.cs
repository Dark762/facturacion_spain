using Facturacion.Configuration.Security;
using Xunit;

namespace Facturacion.UnitTests;

/// <summary>
/// Los nombres viajan dentro de los JWT y las cookies: cambiarlos rompe sesiones existentes.
/// </summary>
public sealed class FacturacionClaimTests
{
    [Theory]
    [InlineData(FacturacionClaim.TenantId, "tenant_id")]
    [InlineData(FacturacionClaim.SecurityStamp, "security_stamp")]
    [InlineData(FacturacionClaim.Purpose, "purpose")]
    [InlineData(FacturacionClaim.ApiToken, "api_token")]
    [InlineData(FacturacionClaim.ApiExpiresUtc, "api_expires_utc")]
    [InlineData(FacturacionClaim.Tenants, "tenants")]
    [InlineData(FacturacionClaim.PlatformAllowed, "platform_allowed")]
    [InlineData(FacturacionClaim.SelectionToken, "selection_token")]
    public void Claim_names_remain_compatible(FacturacionClaim claim, string expected)
        => Assert.Equal(expected, claim.ToClaimType());

    [Theory]
    [InlineData(FacturacionClaimPurpose.TenantSelection, "tenant_selection")]
    [InlineData(FacturacionClaimPurpose.Platform, "platform")]
    public void Purpose_values_remain_compatible(FacturacionClaimPurpose purpose, string expected)
        => Assert.Equal(expected, purpose.ToClaimValue());
}
