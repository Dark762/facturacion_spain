using Microsoft.EntityFrameworkCore;

namespace Facturacion.Infrastructure.Persistence.Providers;

public sealed class SqlServerDatabaseProvider : IDatabaseProvider
{
    public string Name => "SqlServer";

    public void Configure(
        DbContextOptionsBuilder options,
        string connectionString)
    {
        options.UseSqlServer(connectionString);
    }
}
