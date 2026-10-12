using Microsoft.EntityFrameworkCore;

namespace Facturacion.Infrastructure.Persistence.Providers;

public sealed class DatabaseProviderResolver
{
    private readonly IReadOnlyDictionary<string, IDatabaseProvider>
        _providers;

    public DatabaseProviderResolver(
        IEnumerable<IDatabaseProvider> providers)
    {
        var byName = new Dictionary<string, IDatabaseProvider>(
            StringComparer.OrdinalIgnoreCase);

        foreach (var provider in providers)
        {
            if (!byName.TryAdd(provider.Name, provider))
                throw new InvalidOperationException(
                    $"Varios proveedores declaran el nombre '{provider.Name}': " +
                    $"{byName[provider.Name].GetType().Name} y " +
                    $"{provider.GetType().Name}. Los nombres deben ser únicos.");
        }

        _providers = byName;
    }

    public void Configure(
        DbContextOptionsBuilder options,
        string providerName,
        string connectionString)
    {
        if (!_providers.TryGetValue(
            providerName, out var provider))
        {
            throw new NotSupportedException(
                $"Proveedor no registrado: '{providerName}'. " +
                $"Disponibles: {string.Join(", ", _providers.Keys.OrderBy(x => x))}.");
        }

        provider.Configure(options, connectionString);
    }
}
