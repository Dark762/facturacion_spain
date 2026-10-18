using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace Facturacion.Configuration;

/// <summary>
/// Configuracion centralizada por host y entorno.
/// Prioridad (menor a mayor): Shared, Shared.Environment, Host, Host.Environment,
/// User Secrets (Development), variables de entorno y argumentos.
/// </summary>
public static class SharedConfigurationExtensions
{
    public static ConfigurationManager AddFacturacionConfiguration(
        this ConfigurationManager configuration,
        IHostEnvironment environment,
        FacturacionHost host)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        // El host ASP.NET carga appsettings.json por defecto. No permitimos que
        // esos archivos dispersos sobrescriban los valores centralizados.
        for (var i = configuration.Sources.Count - 1; i >= 0; i--)
        {
            if (configuration.Sources[i] is JsonConfigurationSource json &&
                (string.Equals(json.Path, "appsettings.json", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(json.Path, $"appsettings.{environment.EnvironmentName}.json", StringComparison.OrdinalIgnoreCase)))
                configuration.Sources.RemoveAt(i);
        }

        var name = host.ToConfigurationName();
        var env = environment.EnvironmentName;
        var files = new (string Path, bool Required)[]
        {
            ("appsettings.Shared.json", true),
            ($"Environments/appsettings.Shared.{env}.json", false),
            ($"appsettings.{name}.json", true),
            ($"Hosts/{name}/appsettings.{env}.json", false)
        };

        var insertionIndex = 0;
        foreach (var (relative, required) in files)
        {
            var published = Path.Combine(environment.ContentRootPath, relative.Replace('/', Path.DirectorySeparatorChar));
            var repository = Path.GetFullPath(Path.Combine(
                environment.ContentRootPath, "..", "Facturacion.Configuration",
                relative.Replace('/', Path.DirectorySeparatorChar)));
            var path = File.Exists(published) ? published : repository;

            if (!File.Exists(path))
            {
                if (required)
                    throw new FileNotFoundException($"No se encontro {relative}.", path);
                continue;
            }

            configuration.Sources.Insert(insertionIndex++, new JsonConfigurationSource
            {
                Path = Path.GetFileName(path),
                Optional = false,
                ReloadOnChange = true,
                FileProvider = new PhysicalFileProvider(Path.GetDirectoryName(path)!)
            });
        }

        return configuration;
    }
}
