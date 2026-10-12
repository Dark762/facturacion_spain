using Microsoft.EntityFrameworkCore;

namespace Facturacion.Infrastructure.Persistence.Providers;

public interface IDatabaseProvider
{
    string Name { get; }

    void Configure(
        DbContextOptionsBuilder options,
        string connectionString);
}
