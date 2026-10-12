using Microsoft.EntityFrameworkCore;

namespace Facturacion.Infrastructure.Persistence.Providers;

/// <summary>
/// MariaDB se sirve del proveedor de Pomelo, pero debe declararse como
/// MariaDbServerVersion: de lo contrario Pomelo genera DDL de MySQL que puede
/// no ser valido en MariaDB.
/// </summary>
public sealed class MariaDbDatabaseProvider : IDatabaseProvider
{
    public string Name => "MariaDb";

    public void Configure(
        DbContextOptionsBuilder options,
        string connectionString)
    {
        options.UseMySql(
            connectionString,
            ServerVersion.AutoDetect(connectionString));
    }
}