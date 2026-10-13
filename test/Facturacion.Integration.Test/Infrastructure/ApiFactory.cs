using Facturacion.Application.Interfaces;
using Facturacion.Infrastructure.Identity;
using Facturacion.Infrastructure.Billing;
using Facturacion.Infrastructure.Multitenancy;
using Facturacion.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Facturacion.Integration.Test.Infrastructure;

/// <summary>
/// Levanta la API real (Program) sustituyendo MySQL por SQLite, de modo que
/// se ejercita el arranque completo: DI, CORS, JWT y middleware de tenant.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    public const string SigningKey = "integration-test-signing-key-32-bytes-min!!";
    public const string AllowedOrigin = "https://app.example.test";

    /// <summary>
    /// Entorno de pruebas. Distinto de Development para que no se cargen los
    /// user-secrets, que contienen la cadena de conexion real.
    /// </summary>
    public const string TestEnvironment = "Integration";

    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private bool _schemaReady;

    public ApiFactory()
    {
        _connection.Open();
        CreateSchema();
        _schemaReady = true;
    }

protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Entorno propio de integracion, NO Development. Los user-secrets solo
        // se cargan en Development, asi que en este entorno la cadena real de
        // MariaDB no llega a existir dentro del proceso de pruebas.
        builder.UseEnvironment(TestEnvironment);
        builder.UseSetting("Jwt:Issuer", "Facturacion.Api");
        builder.UseSetting("Jwt:Audience", "Facturacion.Client");
        builder.UseSetting("Jwt:ExpiryMinutes", "15");
        builder.UseSetting("Jwt:SigningKey", SigningKey);
        builder.UseSetting("Database:Provider", "Sqlite");
        builder.UseSetting("Database:ConnectionStringName", "Default");
        builder.UseSetting("ConnectionStrings:Default", "DataSource=:memory:");
        builder.UseSetting("Cors:AllowedOrigins:0", AllowedOrigin);

        // Sin user-secrets en este entorno la seccion Email de appsettings.json
        // llega vacia y la API no arranca. Se define aqui con valores
        // ficticios: las pruebas nunca envian correo.
        builder.UseSetting("Email:Provider", "Smtp");
        builder.UseSetting("Email:FromAddress", "test@example.test");
        builder.UseSetting("Email:Smtp:Host", "localhost");
        builder.UseSetting("Email:Smtp:Port", "25");
        builder.UseSetting("Email:VerifyEmailPath", "/verificar-email");

        builder.ConfigureServices(services =>
        {
            // El esquema se crea antes de arrancar el host; solo se configura una vez.
            if (!_schemaReady)
            {
                CreateSchema();
                _schemaReady = true;
            }

            // La emision real de JWT no es el objeto de estas pruebas: se sustituye
            // por un esquema con identidad estable para poder fijar el tenant.
            services.AddAuthentication(TestAuthHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                    TestAuthHandler.SchemeName, _ => { });

            ReplaceWithSqlite<BillingDbContext>(services, _connection);
            ReplaceWithSqlite<AuthDbContext>(services, _connection);

            // SelfServiceDbContext TAMBIEN se registra con la cadena de
            // configuracion (SelfServiceRegistration llama a
            // AddConfiguredDatabaseContext). En pruebas esa cadena resuelve a
            // los user-secrets de Development, es decir la base real, porque
            // UseSetting tiene MENOR precedencia que user-secrets. Si no se
            // sustituye aqui, el primer test que toque un endpoint de
            // self-service escribe en la base de verdad.
            ReplaceWithSqlite<SelfServiceDbContext>(services, _connection);

            // AddSaasAuthentication ya registro TenantContext como concreto y
            // ITenantContext apuntando a esa misma instancia. Hay que conservar
            // ese enlace: dos instancias distintas dejarian el DbContext sin tenant.
            services.RemoveAll<ITenantContext>();
            services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<TenantContext>());
        });
    }

    private void CreateSchema()
    {
        var billing = new BillingDbContext(
            new DbContextOptionsBuilder<BillingDbContext>()
                .UseSqlite(_connection).Options,
            new TenantContext());
        RunCreateScript(billing);

        var auth = new AuthDbContext(
            new DbContextOptionsBuilder<AuthDbContext>()
                .UseSqlite(_connection).Options);
        RunCreateScript(auth);
    }

    /// <summary>
    /// EnsureCreated() omite el segundo contexto porque la base ya existe,
    /// asi que se ejecuta el script de creacion de cada modelo por separado.
    /// </summary>
    private static void RunCreateScript(DbContext context)
    {
        var script = context.Database.GenerateCreateScript();
        if (!string.IsNullOrWhiteSpace(script))
            context.Database.ExecuteSqlRaw(script);
    }

    private static void ReplaceWithSqlite<TContext>(
        IServiceCollection services, SqliteConnection connection)
        where TContext : DbContext
    {
        services.RemoveAll<DbContextOptions<TContext>>();
        services.RemoveAll<DbContextOptions>();
        services.AddDbContext<TContext>(o => o.UseSqlite(connection));
    }

protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) _connection.Dispose();
    }

    /// <summary>
    /// Barrera fail-closed. Se valida contra host.Services, el proveedor REAL
    /// ya construido, y antes de que el cliente pueda hacer ninguna peticion.
    ///
    /// No se usa services.BuildServiceProvider() dentro de ConfigureServices:
    /// construir un segundo contenedor duplica los singletons, rompe los
    /// lifetimes scoped y validaria un objeto distinto del que se usa.
    /// </summary>
    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        TestIsolationGuard.AssertAllContextsAreSqlite(host.Services);
        TestIsolationGuard.AssertNoRealConnectionString(
            host.Services.GetRequiredService<IConfiguration>());

        return host;
    }
}
