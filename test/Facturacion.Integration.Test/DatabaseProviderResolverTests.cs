using Facturacion.Application.Interfaces;
using Facturacion.Configuration;
using Facturacion.Configuration.Options;
using Facturacion.Infrastructure.Persistence;
using Facturacion.Infrastructure.Persistence.Providers;
using Facturacion.Integration.Test.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

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
        using var provider = BuildProvider(new Dictionary<string, string?>());

        var error = Assert.Throws<OptionsValidationException>(() =>
            _ = provider.GetRequiredService<IOptions<DatabaseOptions>>().Value);

        Assert.Contains("Database:Provider", error.Message);
    }

    [Fact]
    public void Missing_connection_string_fails_naming_the_expected_key()
    {
        using var provider = BuildProvider(new Dictionary<string, string?>
        {
            ["Database:Provider"] = "MySql",
            ["Database:ConnectionStringName"] = "Default"
        });

        var error = Assert.Throws<OptionsValidationException>(() =>
            _ = provider.GetRequiredService<IOptions<DatabaseOptions>>().Value);

        Assert.Contains("ConnectionStrings:Default", error.Message);
    }

    [Fact]
    public void Complete_database_settings_resolve_and_carry_the_connection_string()
    {
        using var provider = BuildProvider(new Dictionary<string, string?>
        {
            ["Database:Provider"] = "MySql",
            ["Database:ConnectionStringName"] = "Default",
            ["ConnectionStrings:Default"] = "Server=localhost;Database=test;"
        });

        var options = provider.GetRequiredService<IOptions<DatabaseOptions>>().Value;

        Assert.Equal("MySql", options.Provider);
        Assert.Equal("Server=localhost;Database=test;", options.ConnectionString);
    }

    /// <summary>
    /// Monta el contenedor con la configuracion central. La validacion ocurre al
    /// arrancar (ValidateOnStart), no al resolver el DbContext: el mensaje debe
    /// llegar en el bootstrap y no en la primera peticion.
    /// </summary>
    private static ServiceProvider BuildProvider(Dictionary<string, string?> settings)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ITenantContext>(new BillingDatabaseFixture.StubTenantContext());
        services.AddSingleton<IConfiguration>(
            new ConfigurationBuilder().AddInMemoryCollection(settings).Build());
        services.AddFacturacionOptions(ConfigurationScope.Database);

        return services.BuildServiceProvider();
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
