using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Facturacion.Integration.Test.Infrastructure;

/// <summary>
/// Barrera fail-closed que impide que una prueba escriba fuera del entorno
/// aislado.
///
/// Existe porque la precedencia de configuracion juega en contra: en
/// WebApplicationFactory, builder.UseSetting() alimenta la configuracion del
/// HOST, que es la fuente de MENOR prioridad. Los user-secrets de Development
/// se anaden despues y GANAN. Traducido: la cadena que acaba viendo la
/// aplicacion es la real, no la de la prueba, salvo que el contexto se
/// sustituya explicitamente.
///
/// Por eso no se comprueba la cadena de configuracion: comprobar la
/// CONFIGURACION EFFECTIVA de los contextos ya resueltos. Invariante real:
/// ningun DbContext de una prueba puede apuntar a un motor que no sea SQLite.
/// Asi da igual cuantas fuentes de configuracion haya y en que orden.
/// </summary>
public static class TestIsolationGuard
{
    private const string SqliteProvider = "Microsoft.EntityFrameworkCore.Sqlite";

    public sealed class IsolationException(string message) : InvalidOperationException(message);

    /// <summary>Contextos que toda prueba debe mantener en SQLite.</summary>
    public static IReadOnlyList<Type> GuardedContexts { get; } =
    [
        typeof(Facturacion.Infrastructure.Persistence.BillingDbContext),
        typeof(Facturacion.Infrastructure.Identity.AuthDbContext),
        typeof(Facturacion.Infrastructure.Billing.SelfServiceDbContext)
    ];

    /// <summary>
    /// Falla si algun contexto resuelto no usa SQLite. Se invoca una vez que
    /// construido el contenedor, antes de que ninguna prueba escriba.
    /// </summary>
    public static void AssertAllContextsAreSqlite(IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);

        using var scope = services.CreateScope();
        var offenders = new List<string>();

        foreach (var contextType in GuardedContexts)
        {
            if (scope.ServiceProvider.GetService(contextType) is not DbContext context)
                continue;   // no esta registrado en este fixture: nada que guardar

            var provider = context.Database.ProviderName;
            if (!string.Equals(provider, SqliteProvider, StringComparison.Ordinal))
                offenders.Add($"{contextType.Name} usa '{provider}'");
        }

        if (offenders.Count > 0)
            throw new IsolationException(
                "Una prueba se ejecutaria contra una base de datos REAL: " +
                string.Join("; ", offenders) +
                ". Las pruebas solo pueden usar SQLite en memoria. Sustituye el contexto " +
                "correspondiente en el fixture antes de seguir.");
    }
}