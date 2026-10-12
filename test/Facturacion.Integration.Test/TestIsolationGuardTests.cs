using Facturacion.Infrastructure.Billing;
using Facturacion.Infrastructure.Identity;
using Facturacion.Infrastructure.Persistence;
using Facturacion.Integration.Test.Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Facturacion.Integration.Test;

/// <summary>
/// El aislamiento de las pruebas es una barrera de seguridad: si falla, una
/// prueba puede escribir en la base de datos real. Estas pruebas comprueban
/// que la barrera MUERDE.
/// </summary>
public sealed class TestIsolationGuardTests
{
    private static ServiceProvider BuildContainer(Action<IServiceCollection> configure)
    {
        var services = new ServiceCollection();
        services.AddLogging();

        // BillingDbContext exige ITenantContext; sin el, falla al resolverse y la

        // barrera no llegaria a comprobar el proveedor.

        services.AddScoped<Facturacion.Application.Interfaces.ITenantContext, BillingDatabaseFixture.StubTenantContext>();
        configure(services);
        return services.BuildServiceProvider();
    }

    // ---------- La barrera muerde ----------

    [Fact]
    public void Un_contexto_apuntando_a_MariaDB_es_rechazado()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        using var provider = BuildContainer(s =>
        {
            // Simula el fallo real: cadena de MySQL en lugar de SQLite.
            s.AddDbContext<BillingDbContext>(o => o.UseMySql("Server=localhost;Database=facturacion_saas;User Id=root;Password=root;", new MySqlServerVersion(new Version(10, 11, 0))));
        });

        var ex = Assert.Throws<TestIsolationGuard.IsolationException>(
            () => TestIsolationGuard.AssertAllContextsAreSqlite(provider));

        Assert.Contains("base de datos REAL", ex.Message);
        Assert.Contains("BillingDbContext", ex.Message);
        Assert.DoesNotContain("Password=root", ex.Message);

        connection.Dispose();
    }

    [Fact]
    public void Un_contexto_apuntando_a_SqlServer_es_rechazado()
    {
        using var provider = BuildContainer(s =>
            s.AddDbContext<AuthDbContext>(o => o.UseSqlServer(
                "Server=localhost;Database=facturacion_saas;User Id=sa;Password=x;")));

        Assert.Throws<TestIsolationGuard.IsolationException>(
            () => TestIsolationGuard.AssertAllContextsAreSqlite(provider));
    }

    [Fact]
    public void Un_contexto_apuntando_a_PostgreSQL_es_rechazado()
    {
        using var provider = BuildContainer(s =>
            s.AddDbContext<SelfServiceDbContext>(o => o.UseNpgsql(
                "Host=localhost;Database=facturacion_saas;Username=postgres;Password=x;")));

        Assert.Throws<TestIsolationGuard.IsolationException>(
            () => TestIsolationGuard.AssertAllContextsAreSqlite(provider));
    }

    [Fact]
    public void Basta_un_contexto_contaminado_para_que_toda_la_prueba_falle()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        using var provider = BuildContainer(s =>
        {
            s.AddDbContext<BillingDbContext>(o => o.UseSqlite(connection));
            s.AddDbContext<AuthDbContext>(o => o.UseSqlite(connection));
            // SelfService se queda fuera de la sustitucion: es exactamente el
            // olvido que produjo el riesgo.
            s.AddDbContext<SelfServiceDbContext>(o => o.UseMySql("Server=localhost;Database=facturacion_saas;User Id=root;Password=root;", new MySqlServerVersion(new Version(10, 11, 0))));
        });

        Assert.Throws<TestIsolationGuard.IsolationException>(
            () => TestIsolationGuard.AssertAllContextsAreSqlite(provider));

        connection.Dispose();
    }

    // ---------- Y no muerde de mas ----------

    [Fact]
    public void Todos_los_contextos_en_SQLite_pasan_la_comprobacion()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        using var provider = BuildContainer(s =>
        {
            s.AddDbContext<BillingDbContext>(o => o.UseSqlite(connection));
            s.AddDbContext<AuthDbContext>(o => o.UseSqlite(connection));
            s.AddDbContext<SelfServiceDbContext>(o => o.UseSqlite(connection));
        });

        TestIsolationGuard.AssertAllContextsAreSqlite(provider);

        connection.Dispose();
    }

    [Fact]
    public void Un_fixture_que_no_registra_un_contexto_no_falla()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        using var provider = BuildContainer(s =>
            s.AddDbContext<AuthDbContext>(o => o.UseSqlite(connection)));

        TestIsolationGuard.AssertAllContextsAreSqlite(provider);

        connection.Dispose();
    }
}