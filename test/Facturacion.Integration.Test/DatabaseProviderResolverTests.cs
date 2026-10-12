using Facturacion.Application.Interfaces;
using Facturacion.Infrastructure.Persistence;
using Facturacion.Infrastructure.Persistence.Providers;
using Facturacion.Integration.Test.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Facturacion.Integration.Test;

/// <summary>
/// El resolutor de proveedores se registra dos veces al configurar
/// BillingDbContext y AuthDbContext: si vuelve a duplicar, el contenedor
/// revienta al construir cualquier DbContext.
/// </summary>
public sealed class DatabaseProviderResolverTests
{
    [Theory]
    [InlineData("MySql")]
    [InlineData("mysql")]
    [InlineData("MYSQL")]
    [InlineData("MySqL")]
    public void Provider_name_is_matched_case_insensitively(string requested)
    {
        var mysql = new RecordingProvider("MySql");
        var sqlServer = new RecordingProvider("SqlServer");
        var resolver = new DatabaseProviderResolver(
            new IDatabaseProvider[] { mysql, sqlServer });

        resolver.Configure(new DbContextOptionsBuilder(), requested, "cadena");

        Assert.True(mysql.Configured);
        Assert.False(sqlServer.Configured);
    }

    [Fact]
    public void Unknown_provider_throws_and_lists_the_available_ones()
    {
        var resolver = new DatabaseProviderResolver(
            new IDatabaseProvider[] { new RecordingProvider("MySql") });

        var error = Assert.Throws<NotSupportedException>(() =>
            resolver.Configure(new DbContextOptionsBuilder(), "Oracle", "x"));

        Assert.Contains("MySql", error.Message);
    }

    [Fact]
    public void Two_providers_sharing_a_name_are_rejected_with_a_clear_message()
    {
        var error = Assert.Throws<InvalidOperationException>(() =>
            _ = new DatabaseProviderResolver(new IDatabaseProvider[]
            {
                new RecordingProvider("MySql"),
                new RecordingProvider("MySql")
            }));

        Assert.Contains("MySql", error.Message);
    }

    [Fact]
    public void AddDatabaseProviders_discovers_the_real_providers()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDatabaseProviders();

        var names = services.BuildServiceProvider()
            .GetServices<IDatabaseProvider>()
            .Select(p => p.Name).ToList();

        Assert.Contains("MySql", names);
        Assert.Contains("SqlServer", names);
    }

    [Fact]
    public void AddDatabaseProviders_is_idempotent()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        // Se llama dos veces, igual que en Program.cs + AddSaasAuthentication.
        services.AddDatabaseProviders();
        services.AddDatabaseProviders();

        var providers = services.BuildServiceProvider()
            .GetServices<IDatabaseProvider>().ToList();

        Assert.NotEmpty(providers);
        Assert.Equal(
            providers.Select(p => p.GetType()).Distinct().Count(),
            providers.Count);
    }

    [Fact]
    public void Resolver_is_usable_after_a_double_registration()
    {
        // Regresion directa del fallo encontrado al arrancar la API: dos
        // instancias de MySqlDatabaseProvider hacian saltar el constructor.
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDatabaseProviders();
        services.AddDatabaseProviders();

        var resolver = services.BuildServiceProvider()
            .GetRequiredService<DatabaseProviderResolver>();

        Assert.NotNull(resolver);
    }

    [Fact]
    public void Missing_provider_setting_fails_with_an_actionable_message()
    {
        var services = NewServices();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection().Build();

        services.AddConfiguredDatabaseContext<BillingDbContext>(configuration);

        var error = Assert.Throws<InvalidOperationException>(() =>
        {
            using var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();
            _ = scope.ServiceProvider.GetRequiredService<BillingDbContext>();
        });

        Assert.Contains("Database:Provider", error.Message);
    }

    [Fact]
    public void Missing_connection_string_fails_naming_the_expected_key()
    {
        var services = NewServices();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:Provider"] = "MySql",
                ["Database:ConnectionStringName"] = "Default"
            })
            .Build();

        services.AddConfiguredDatabaseContext<BillingDbContext>(configuration);

        var error = Assert.Throws<InvalidOperationException>(() =>
        {
            using var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();
            _ = scope.ServiceProvider.GetRequiredService<BillingDbContext>();
        });

        Assert.Contains("ConnectionStrings:Default", error.Message);
    }

    /// <summary>BillingDbContext exige ITenantContext en su constructor.</summary>
    private static IServiceCollection NewServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ITenantContext>(
            new BillingDatabaseFixture.StubTenantContext());
        return services;
    }

    private sealed class RecordingProvider : IDatabaseProvider
    {
        public RecordingProvider(string name) => Name = name;

        public string Name { get; }
        public bool Configured { get; private set; }

        public void Configure(DbContextOptionsBuilder options, string connectionString)
            => Configured = true;
    }
}
