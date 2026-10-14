using Facturacion.Configuration.Options;
using Facturacion.Configuration.Validation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Facturacion.Configuration;

/// <summary>
/// Registra y valida toda la configuracion tipada de la solucion.
///
/// Un solo sitio donde se sabe que claves existen, cuales son obligatorias y
/// como se validan. El resto de la aplicacion pide <c>IOptions&lt;T&gt;</c> y
/// nunca toca <see cref="IConfiguration"/> ni escribe claves a mano.
/// </summary>
public static class ConfigurationServiceCollectionExtensions
{
    /// <summary>
    /// Enlaza cada seccion con su contrato y valida al arrancar. Las secciones
    /// marcadas en <paramref name="scope"/> son obligatorias: si falta algo el
    /// host muere nombrando la clave. El resto se apaga con un aviso.
    /// </summary>
    public static IServiceCollection AddFacturacionOptions(
        this IServiceCollection services, ConfigurationScope scope)
    {
        // La cadena de conexion vive en ConnectionStrings:&lt;nombre&gt;, no en la
        // seccion Database. Se copia ya resuelta para que el registro de los
        // DbContext no dependa de IConfiguration.
        services.AddOptions<DatabaseOptions>()
            .BindConfiguration(DatabaseOptions.SectionName)
            .Configure<IConfiguration>((options, configuration) =>
            {
                var name = string.IsNullOrWhiteSpace(options.ConnectionStringName)
                    ? DatabaseOptions.DefaultConnectionStringName
                    : options.ConnectionStringName;
                options.ConnectionString = configuration.GetConnectionString(name) ?? "";
            });

        // Siempre obligatorio. El worker recorre la cola de varios clientes:
        // sin base de datos, arrancar significaria vaciar una cola ajena.
        services.AddSingleton<IValidateOptions<DatabaseOptions>, DatabaseOptionsValidator>();

        Bind<JwtOptions>(services, JwtOptions.SectionName,
            scope.Has(ConfigurationScope.Jwt), new JwtOptionsValidator());
        Bind<OutboxOptions>(services, OutboxOptions.SectionName,
            scope.Has(ConfigurationScope.Outbox), new OutboxOptionsValidator());
        Bind<InvitationOptions>(services, InvitationOptions.SectionName,
            scope.Has(ConfigurationScope.Invitations), new InvitationOptionsValidator());

        // Siempre enlazadas, nunca obligatorias: la ausencia es una decision
        // del despliegue, no un error.
        services.AddOptions<StripeOptions>().BindConfiguration(StripeOptions.SectionName);
        services.AddSingleton<IValidateOptions<StripeOptions>, StripeOptionsValidator>();

        services.AddOptions<EmailOptions>().BindConfiguration(EmailOptions.SectionName);
        services.AddSingleton<IValidateOptions<EmailOptions>, EmailOptionsValidator>();

        services.AddOptions<CorsOptions>().BindConfiguration(CorsOptions.SectionName);
        services.AddOptions<IdentityPolicyOptions>().BindConfiguration(IdentityPolicyOptions.SectionName);
        services.AddOptions<FiscalSchemaOptions>().BindConfiguration(FiscalSchemaOptions.SectionName);

        return services;
    }

    /// <summary>
    /// Enlaza una seccion y la valida al arrancar (<c>ValidateOnStart</c>): el
    /// error sale en el bootstrap, nombrando la clave, y no en la primera
    /// peticion que la necesite.
    /// </summary>
    private static void Bind<TOptions>(
        IServiceCollection services, string sectionName, bool required, IValidateOptions<TOptions> validator)
        where TOptions : class
    {
        var builder = services.AddOptions<TOptions>().BindConfiguration(sectionName);
        services.AddSingleton(validator);

        if (required) builder.ValidateOnStart();
    }
}

internal static class ConfigurationScopeExtensions
{
    /// <summary>Indica si el scope incluye esta seccion obligatoria.</summary>
    public static bool Has(this ConfigurationScope scope, ConfigurationScope flag) =>
        (scope & flag) == flag;
}