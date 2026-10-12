using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace Facturacion.Integration.Test.Infrastructure;

/// <summary>
/// Barrera fail-closed que impide que una prueba escriba fuera del entorno
/// aislado.
///
/// POR QUE EXISTE. En WebApplicationFactory, builder.UseSetting() alimenta la
/// configuracion del HOST, que es la fuente de MENOR precedencia. Los
/// user-secrets de Development se anaden despues y GANAN. Traducido: la cadena
/// de conexion que acaba viendo la aplicacion es la real, no la de la prueba.
///
/// Que se compruebe el PROVEEDOR REAL y no la configuracion: sustituir
/// contextos uno a uno es fragil, basta olvidar uno y la filtracion vuelve.
/// Invariante comprobada: todo DbContext protegido resuelve a SQLite.
///
/// CUANDO SE VALIDA. Contra host.Services, ya construido, antes de la primera
/// peticion. Construir un segundo contenedor con services.BuildServiceProvider()
/// dentro de ConfigureServices es un antipatron: duplica los singletons,
/// rompe los lifetimes scoped y valida un objeto que no es el que se usa.
///
/// FAIL-CLOSED. Un contexto protegido que NO se resuelve tambien es un fallo.
/// Si el guard se limitara a comprobar los que encuentra, bastaria con quitar
/// un registro del contenedor para desactivar la proteccion sin que nadie se
/// entere.
/// </summary>
public static class TestIsolationGuard
{
    private const string SqliteProvider = "Microsoft.EntityFrameworkCore.Sqlite";

    public sealed class IsolationException(string message) : InvalidOperationException(message);

    /// <summary>
    /// Contextos que la aplicacion usa contra la base. Los tres DEBEN existir
    /// y DEBEN usar SQLite. Un contexto ausente se considera contaminacion.
    /// </summary>
    public static IReadOnlyList<Type> GuardedContexts { get; } =
    [
        typeof(Facturacion.Infrastructure.Persistence.BillingDbContext),
        typeof(Facturacion.Infrastructure.Identity.AuthDbContext),
        typeof(Facturacion.Infrastructure.Billing.SelfServiceDbContext)
    ];

    public static void AssertAllContextsAreSqlite(IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);

        using var scope = services.CreateScope();
        var problems = new List<string>();

        foreach (var contextType in GuardedContexts)
        {
            DbContext? context;
            try
            {
                context = scope.ServiceProvider.GetService(contextType) as DbContext;
            }
            catch (Exception ex)
            {
                problems.Add($"{contextType.Name} no se puede construir: {ex.GetType().Name}");
                continue;
            }

            // Fail-closed: que no este registrado NO es aceptable.
            if (context is null)
            {
                problems.Add($"{contextType.Name} NO esta registrado en el contenedor de pruebas");
                continue;
            }

            var provider = context.Database.ProviderName;
            if (!string.Equals(provider, SqliteProvider, StringComparison.Ordinal))
                problems.Add($"{contextType.Name} usa '{provider}' en lugar de SQLite");
        }

        if (problems.Count > 0)
            throw new IsolationException(
                "Las pruebas se ejecutarian contra una base de datos REAL. " +
                string.Join(" | ", problems) +
                ". Toda prueba debe usar SQLite en memoria. Sustituye el contexto " +
                "correspondiente en el fixture antes de seguir.");
    }

/// <summary>
    /// Comprueba que la configuracion EFECTIVA no contiene una conexion real.
    ///
    /// No se comprueba el nombre del proveedor: appsettings.json declara
    /// "Database:Provider": "MySql" y ese fichero se carga DESPUES que
    /// UseSetting, asi que gana. El nombre no dice nada sobre a donde se
    /// conecta de verdad.
    ///
    /// Si se mira la cadena efectiva: en el entorno Integration no se cargan
    /// los user-secrets de Development, de modo que la cadena real no llega a
    /// existir dentro del proceso de pruebas. Solo puede quedar vacia o
    /// apuntando a memoria.
    /// </summary>
    public static void AssertNoRealConnectionString(IConfiguration configuration)
    {
        var connection = configuration.GetConnectionString("Default");

        if (string.IsNullOrWhiteSpace(connection))
            return;

        var lowered = connection.ToLowerInvariant();

        // Marcadores de un motor servidor. "DataSource=:memory:" NO aparece.
        string[] realServerMarkers =
        [
            "server=", "host=", "data source=tcp", "datasource=tcp",
            "localhost", "127.0.0.1", "password=", "pwd=", "user id=", "uid="
        ];

        foreach (var marker in realServerMarkers)
        {
            if (lowered.Contains(marker, StringComparison.Ordinal))
                throw new IsolationException(
                    $"La configuracion efectiva contiene una conexion real en " +
                    $"ConnectionStrings:Default (marcador '{marker}'). Las pruebas solo " +
                    "pueden usar SQLite en memoria.");
        }
    }
}