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
    public static ConfigurationManager AddFacturacionConfiguration(
        this ConfigurationManager configuration, IHostEnvironment environment, string application)
    {
        var names = new[] { "appsettings.Shared.json", $"appsettings.{application}.json" };
        for (var index = 0; index < names.Length; index++)
        {
            var name = names[index];
            var published = Path.Combine(environment.ContentRootPath, name);
            var repository = Path.GetFullPath(Path.Combine(
                environment.ContentRootPath, "..", "Facturacion.Configuration", name));
            var path = File.Exists(published) ? published : repository;
            if (!File.Exists(path))
                throw new FileNotFoundException($"No se encontró {name}.", path);

            configuration.Sources.Insert(index, new JsonConfigurationSource
            {
                Path = name,
                Optional = false,
                ReloadOnChange = true,
                FileProvider = new PhysicalFileProvider(Path.GetDirectoryName(path)!)
            });
        }
        return configuration;
    }
}
