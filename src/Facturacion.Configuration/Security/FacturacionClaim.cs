namespace Facturacion.Configuration.Security;

/// <summary>
/// Claims propias de Facturación (no estándar JWT) compartidas por la API y la web.
/// Usa <see cref="FacturacionClaimExtensions.ToClaimType"/> para obtener el nombre
/// que viaja en el token o en la cookie.
/// </summary>
public enum FacturacionClaim
{
    /// <summary><c>tenant_id</c>: tenant activo del usuario.</summary>
    TenantId,

    /// <summary><c>security_stamp</c>: sello de seguridad usado para revocar sesiones.</summary>
    SecurityStamp,

    /// <summary><c>purpose</c>: propósito del token (ver <see cref="FacturacionClaimPurpose"/>).</summary>
    Purpose,

    /// <summary><c>api_token</c>: access token de la API guardado en la cookie de la web.</summary>
    ApiToken,

    /// <summary><c>api_expires_utc</c>: caducidad del access token de la API.</summary>
    ApiExpiresUtc,

    /// <summary><c>tenants</c>: tenants disponibles durante la selección.</summary>
    Tenants,

    /// <summary><c>platform_allowed</c>: indica si el usuario puede acceder a la plataforma.</summary>
    PlatformAllowed,

    /// <summary><c>selection_token</c>: token de preautenticación para elegir tenant.</summary>
    SelectionToken
}

/// <summary>Valores posibles de la claim <see cref="FacturacionClaim.Purpose"/>.</summary>
public enum FacturacionClaimPurpose
{
    /// <summary><c>tenant_selection</c>: token válido solo para completar el login.</summary>
    TenantSelection,

    /// <summary><c>platform</c>: token válido solo para rutas de plataforma.</summary>
    Platform
}

public static class FacturacionClaimExtensions
{
    /// <summary>Nombre con el que la claim viaja en el token o la cookie.</summary>
    public static string ToClaimType(this FacturacionClaim claim) => claim switch
    {
        FacturacionClaim.TenantId => "tenant_id",
        FacturacionClaim.SecurityStamp => "security_stamp",
        FacturacionClaim.Purpose => "purpose",
        FacturacionClaim.ApiToken => "api_token",
        FacturacionClaim.ApiExpiresUtc => "api_expires_utc",
        FacturacionClaim.Tenants => "tenants",
        FacturacionClaim.PlatformAllowed => "platform_allowed",
        FacturacionClaim.SelectionToken => "selection_token",
        _ => throw new ArgumentOutOfRangeException(nameof(claim), claim, "Claim no soportada.")
    };

    /// <summary>Valor con el que el propósito viaja en la claim <c>purpose</c>.</summary>
    public static string ToClaimValue(this FacturacionClaimPurpose purpose) => purpose switch
    {
        FacturacionClaimPurpose.TenantSelection => "tenant_selection",
        FacturacionClaimPurpose.Platform => "platform",
        _ => throw new ArgumentOutOfRangeException(nameof(purpose), purpose, "Propósito de token no soportado.")
    };
}
