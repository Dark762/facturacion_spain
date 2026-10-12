using Microsoft.EntityFrameworkCore;

namespace Facturacion.Infrastructure.Persistence.Providers;

public sealed class PostgreSqlDatabaseProvider : IDatabaseProvider
{
    public string Name => "PostgreSql";

    public void Configure(
        DbContextOptionsBuilder options,
        string connectionString)
    {
        options.UseNpgsql(connectionString);
    }
}