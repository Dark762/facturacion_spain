namespace Facturacion.Configuration.Options;

/// <summary>Política de inactividad de la sesión web, configurable por entorno.</summary>
public sealed class SessionOptions
{
    public const string SectionName = "Session";
    public int IdleTimeoutMinutes { get; set; } = 15;
    public int WarningBeforeSeconds { get; set; } = 60;
    public int HeartbeatIntervalSeconds { get; set; } = 60;
}
