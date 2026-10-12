using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Facturacion.Infrastructure.Persistence.Providers;

public static class DatabaseProviderRegistration
{
    public const string DefaultConnectionStringName = "Default";

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

    public static IServiceCollection AddConfiguredDatabaseContext<TContext>(
        this IServiceCollection services, IConfiguration configuration)
        where TContext : DbContext
    {
        services.AddDatabaseProviders();

        services.AddDbContext<TContext>((serviceProvider, options) =>
        {
            var resolver = serviceProvider
                .GetRequiredService<DatabaseProviderResolver>();

            var providerName = configuration["Database:Provider"];
            if (string.IsNullOrWhiteSpace(providerName))
                throw new InvalidOperationException(
                    "Falta la configuración 'Database:Provider'.");

            var connectionName = configuration["Database:ConnectionStringName"];
            if (string.IsNullOrWhiteSpace(connectionName))
                connectionName = DefaultConnectionStringName;

            var connectionString = configuration.GetConnectionString(connectionName);
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException(
                    $"Falta la cadena de conexión 'ConnectionStrings:{connectionName}'.");

            resolver.Configure(options, providerName, connectionString);
        });

        return services;
    }
}
