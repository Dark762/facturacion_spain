namespace Facturacion.Configuration.Options;

/// <summary>Origenes permitidos por CORS. Vacio significa cerrar la puerta.</summary>
public sealed class CorsOptions
{
    public const string SectionName = "Cors";

    public string[] AllowedOrigins { get; set; } = [];

    /// <summary>
    /// Sin origenes declarados no se permite ninguno. Es deliberado: un
    /// despliegue que olvida la lista no debe quedar abierto por defecto.
    /// </summary>
    public bool IsClosed => AllowedOrigins.Length == 0;
}