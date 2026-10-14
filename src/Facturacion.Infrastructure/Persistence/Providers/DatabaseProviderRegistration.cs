using Facturacion.Configuration.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Facturacion.Infrastructure.Persistence.Providers;

public static class DatabaseProviderRegistration
{
    public static IServiceCollection AddDatabaseProviders(
        this IServiceCollection services)
    {
        var providerTypes = typeof(IDatabaseProvider)
            .Assembly
            .GetTypes()
            .Where(t =>
                t.IsClass &&
                !t.IsAbstract &&
                typeof(IDatabaseProvider).IsAssignableFrom(t))
            .OrderBy(t => t.FullName, StringComparer.Ordinal);

        foreach (var type in providerTypes)
        {
            // TryAddEnumerable evita duplicar si se registra mas de una vez,
            // por ejemplo al configurar BillingDbContext y AuthDbContext.
            services.TryAddEnumerable(
                ServiceDescriptor.Singleton(typeof(IDatabaseProvider), type));
        }

        services.TryAddSingleton<DatabaseProviderResolver>();

        return services;
    }

    /// <summary>
    /// Configura un DbContext contra la base declarada en <see cref="DatabaseOptions"/>.
    ///
    /// Ya no se lee IConfiguration aqui ni se valida nada a mano: la resolucion
    /// de la cadena y sus reglas viven en el validador central, que corre al
    /// arrancar. Aqui solo se traducen los valores ya validados a EF Core.
    /// </summary>
    public static IServiceCollection AddConfiguredDatabaseContext<TContext>(
        this IServiceCollection services)
        where TContext : DbContext
    {
        services.AddDatabaseProviders();

        services.AddDbContext<TContext>((serviceProvider, options) =>
        {
            var database = serviceProvider.GetRequiredService<IOptions<DatabaseOptions>>().Value;
            var resolver = serviceProvider.GetRequiredService<DatabaseProviderResolver>();

            resolver.Configure(options, database.Provider, database.ConnectionString);
        });

        return services;
    }
}
