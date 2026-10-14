using Facturacion.Configuration.Options;
using Microsoft.Extensions.Options;

namespace Facturacion.Configuration.Validation;

/// <summary>
/// Base de datos. Obligatoria: sin ella no hay BillingDbContext, y arrancar
/// serviria para atender peticiones que fallarian al primer uso.
/// </summary>
public sealed class DatabaseOptionsValidator : IValidateOptions<DatabaseOptions>
{
    public ValidateOptionsResult Validate(string? name, DatabaseOptions options)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.Provider))
            failures.Add($"Falta la configuracion '{DatabaseOptions.SectionName}:Provider' " +
                         "(MariaDb, MySql, PostgreSql o SqlServer).");

        if (string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            var connectionName = string.IsNullOrWhiteSpace(options.ConnectionStringName)
                ? DatabaseOptions.DefaultConnectionStringName
                : options.ConnectionStringName;
            failures.Add($"Falta la cadena de conexion " +
                         $"'ConnectionStrings:{connectionName}'.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}