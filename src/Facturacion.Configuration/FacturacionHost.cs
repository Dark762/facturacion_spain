namespace Facturacion.Configuration;

/// <summary>
/// Hosts de la solucion. Cada uno tiene su archivo appsettings.&lt;nombre&gt;.json.
/// Se tipa en vez de pasar cadenas sueltas para que un nombre mal escrito falle
/// al compilar y no al arrancar.
/// </summary>
public enum FacturacionHost
{
    Api,
    Workers,
    WebApp,
    WebPublic
}

public static class FacturacionHostExtensions
{
    /// <summary>Todos los hosts, para pruebas que validan los JSON.</summary>
    public static IReadOnlyList<FacturacionHost> All { get; } =
        [FacturacionHost.Api, FacturacionHost.Workers, FacturacionHost.WebApp, FacturacionHost.WebPublic];

    /// <summary>Base del archivo de configuracion: appsettings.&lt;nombre&gt;.json.</summary>
    public static string ToConfigurationFileName(this FacturacionHost host) =>
        $"appsettings.{host.ToConfigurationName()}.json";

    /// <summary>Base del archivo de configuracion: Api, Workers, Web.App, Web.Public.</summary>
    public static string ToConfigurationName(this FacturacionHost host) => host switch
    {
        FacturacionHost.Api => "Api",
        FacturacionHost.Workers => "Workers",
        FacturacionHost.WebApp => "Web.App",
        FacturacionHost.WebPublic => "Web.Public",
        _ => throw new ArgumentOutOfRangeException(nameof(host), host, "Host desconocido.")
    };
}