using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace Facturacion.Configuration;

/// <summary>
/// Inserta opciones comunes como configuración de menor prioridad.
/// Los appsettings de cada host, User Secrets y variables de entorno prevalecen.
/// </summary>
public static class SharedConfigurationExtensions
{
    public static ConfigurationManager AddFacturacionSharedConfiguration(
        this ConfigurationManager configuration, IHostEnvironment environment)
    {
        var source = new JsonConfigurationSource
        {
            Path = "appsettings.Shared.json",
            Optional = false,
            ReloadOnChange = true,
            FileProvider = new PhysicalFileProvider(environment.ContentRootPath)
        };

        // El host ya cargó sus fuentes. Insertar al principio preserva la
        // prioridad de los ajustes del proyecto, secretos y entorno.
        configuration.Sources.Insert(0, source);
        return configuration;
    }
}
