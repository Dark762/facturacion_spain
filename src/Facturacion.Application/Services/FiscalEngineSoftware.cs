namespace Facturacion.Application.Services;

/// <summary>
/// Identificacion del sistema informatico de facturacion.
/// La especificacion exige identificar el software y su version en cada
/// registro. Estos valores son de la aplicacion, no de la norma: se centralizan
/// aqui para que el cambio de version sea explicito y quede en el repositorio.
/// </summary>
public static class FiscalEngineSoftware
{
    public const string Name = "FacturacionSaaS";

    public const string Version = "0.1.0";

    /// <summary>
    /// Identificador de la instalacion. El valor por defecto es un marcador
    /// explicito: debe sustituirse por el identificador real antes de emitir,
    /// porque forma parte del contenido del registro fiscal.
    /// </summary>
    public const string InstallationId = "PENDIENTE-DE-CONFIGURAR";
}