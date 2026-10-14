using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace Facturacion.Configuration;

/// <summary>
/// Inserta los appsettings de cada host como configuracion de menor prioridad.
/// Los appsettings de cada host, User Secrets y variables de entorno prevalecen.
/// </summary>
public static class SharedConfigurationExtensions
{
    /// <summary>
    /// Carga appsettings.Shared.json y appsettings.&lt;host&gt;.json.
    /// Se resuelven primero junto al binario (publicado) y despues en el
    /// repositorio, para que el proyecto funcione sin copiar nada en local.
    /// Si falta alguno se falla aqui: arrancar sin configuracion es peor que
    /// no arrancar, porque parece que todo va bien.
    /// </summary>
    public static ConfigurationManager AddFacturacionConfiguration(
        this ConfigurationManager configuration,
        IHostEnvironment environment,
        FacturacionHost host)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        var names = new[] { SharedFileName, host.ToConfigurationFileName() };

        for (var index = 0; index < names.Length; index++)
        {
            var name = names[index];
            var published = Path.Combine(environment.ContentRootPath, name);
            var repository = Path.GetFullPath(Path.Combine(
                environment.ContentRootPath, "..", "Facturacion.Configuration", name));
            var path = File.Exists(published) ? published : repository;

            if (!File.Exists(path))
                throw new FileNotFoundException($"No se encontro {name}.", path);

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

    private const string SharedFileName = "appsettings.Shared.json";
}