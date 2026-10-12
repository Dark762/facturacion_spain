using Xunit;

namespace Facturacion.Integration.Test;

/// <summary>
/// El entorno por defecto de `dotnet run` no debe permitir tocar la base de
/// datos real.
///
/// En Development se cargan los user-secrets, que guardan la cadena de
/// conexion real, y appsettings.Development.json fija el proveedor MySql.
/// Arrancar en Development es por tanto una operacion de riesgo sobre datos
/// reales, y por eso los perfiles de arranque usan el entorno Local.
/// </summary>
public sealed class LocalEnvironmentTests
{
    private static string ApiRoot => Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "Facturacion.Api"));

    private static string ReadJson(string relativePath) =>
        File.ReadAllText(Path.Combine(ApiRoot, relativePath));

    [Fact]
    public void Los_perfiles_de_arranque_no_fuerzan_Development()
    {
        var settings = ReadJson(Path.Combine("Properties", "launchSettings.json"));

        Assert.DoesNotContain("\"ASPNETCORE_ENVIRONMENT\": \"Development\"", settings);
    }

    [Fact]
    public void El_entorno_local_no_usa_un_servidor_de_base_de_datos()
    {
        using var doc = System.Text.Json.JsonDocument.Parse(ReadJson("appsettings.Local.json"));

        var provider = doc.RootElement.GetProperty("Database").GetProperty("Provider").GetString();
        var connection = doc.RootElement.GetProperty("ConnectionStrings").GetProperty("Default").GetString();

        // Un servidor real aqui significa que `dotnet run` puede escribir en el.
        Assert.Equal("Sqlite", provider);
        Assert.DoesNotContain("Server=", connection, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Data Source=", connection, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void El_entorno_local_deja_la_ruta_de_los_esquemas_configurable()
    {
        using var doc = System.Text.Json.JsonDocument.Parse(ReadJson("appsettings.Local.json"));

        Assert.Equal("docs/verifactu/xsd",
            doc.RootElement.GetProperty("Fiscal").GetProperty("SchemaDirectory").GetString());
    }
}