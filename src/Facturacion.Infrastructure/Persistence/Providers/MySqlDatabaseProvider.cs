using Microsoft.EntityFrameworkCore;

namespace Facturacion.Infrastructure.Persistence.Providers;

public sealed class MySqlDatabaseProvider : IDatabaseProvider
{
    public string Name => "MySql";

    public void Configure(
        DbContextOptionsBuilder options,
        string connectionString)
    {
        options.UseMySql(
            connectionString,
            ServerVersion.AutoDetect(connectionString));
    }
}