namespace Facturacion.Configuration.Options;

/// <summary>Firma y caducidad de los tokens de la API.</summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "";
    public string Audience { get; set; } = "";

    /// <summary>Clave de firma. Nunca se versiona: llega por entorno o secretos.</summary>
    public string SigningKey { get; set; } = "";

    public int ExpiryMinutes { get; set; } = 15;
}