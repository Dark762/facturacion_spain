namespace Facturacion.Configuration.Options;

/// <summary>
/// Politica de contrasenas y bloqueo de Identity. Compartida para que la API y
/// la web publica apliquen exactamente las mismas reglas: si divergen, un
/// usuario puede(validar una contrasena en un sitio que el otro rechaza).
/// </summary>
public sealed class IdentityPolicyOptions
{
    public const string SectionName = "Identity:Policy";

    public int RequiredLength { get; set; } = 12;
    public bool RequireDigit { get; set; } = true;
    public bool RequireUppercase { get; set; } = true;
    public bool RequireNonAlphanumeric { get; set; } = true;
    public bool RequireUniqueEmail { get; set; } = true;
    public int LockoutMinutes { get; set; } = 15;
    public int MaxFailedAccessAttempts { get; set; } = 5;
}