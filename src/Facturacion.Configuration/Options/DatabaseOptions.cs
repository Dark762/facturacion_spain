namespace Facturacion.Configuration.Options;

/// <summary>
/// Resolucion de la base de datos. La cadena de conexion no se lee de la
/// seccion <see cref="SectionName"/> sino de ConnectionStrings:&lt;nombre&gt;,
/// y se copia aqui resuelta para que el resto del codigo no necesite IConfiguration.
/// </summary>
public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    /// <summary>Nombre de ConnectionStrings que se usa si no se indica otro.</summary>
    public const string DefaultConnectionStringName = "Default";

    /// <summary>MariaDb, MySql, PostgreSql o SqlServer.</summary>
    public string Provider { get; set; } = "";

    /// <summary>Clave dentro de ConnectionStrings.</summary>
    public string ConnectionStringName { get; set; } = DefaultConnectionStringName;

    /// <summary>Cadena ya resuelta. La rellena AddFacturacionOptions.</summary>
    public string ConnectionString { get; set; } = "";
}