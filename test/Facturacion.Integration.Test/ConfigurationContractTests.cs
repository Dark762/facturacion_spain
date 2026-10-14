using Facturacion.Configuration;
using Facturacion.Configuration.Options;
using Facturacion.Configuration.Validation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Facturacion.Integration.Test;

/// <summary>
/// Fija el contrato de la configuracion central: una clave por seccion, sin
/// duplicados ni literales sueltos, y con los host nombrados de forma tipada.
///
/// Sin esto, "Stripe" volveria a aparecer escrito a mano en dos sitios y la
/// primera mitad se desincronizaria sin que nada se entere.
/// </summary>
public sealed class ConfigurationContractTests
{
    public static TheoryData<Type, string> Sections() => new()
    {
        { typeof(DatabaseOptions), "Database" },
        { typeof(JwtOptions), "Jwt" },
        { typeof(StripeOptions), "Stripe" },
        { typeof(EmailOptions), "Email" },
        { typeof(OutboxOptions), "Outbox" },
        { typeof(InvitationOptions), "Invitations" },
        { typeof(CorsOptions), "Cors" },
        { typeof(IdentityPolicyOptions), "Identity:Policy" },
        { typeof(FiscalSchemaOptions), "Fiscal" },
    };

    [Theory]
    [MemberData(nameof(Sections))]
    public void Cada_seccion_declara_su_nombre(Type optionsType, string expected)
    {
        var field = optionsType.GetField("SectionName");

        Assert.NotNull(field);
        Assert.Equal(expected, field.GetRawConstantValue());
    }

    [Fact]
    public void No_hay_dos_secciones_con_el_mismo_nombre()
    {
        var names = Sections()
            .Select(row => (string)row[1]!)
            .ToList();

        Assert.Equal(names.Count, names.Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    [Fact]
    public void Cada_host_tiene_su_archivo_y_el_registro_es_estable()
    {
        foreach (var host in FacturacionHostExtensions.All)
        {
            var name = host.ToConfigurationFileName();
            Assert.Equal($"appsettings.{host.ToConfigurationName()}.json", name);
        }

        // Los nombres no pueden repetirse entre hosts.
        var files = FacturacionHostExtensions.All.Select(h => h.ToConfigurationFileName()).ToList();
        Assert.Equal(files.Count, files.Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    // ---------- Obligatorio: falla con el nombre de la clave ----------

    [Fact]
    public void Database_incompleta_falla_nombrando_la_clave()
    {
        var error = Validate<DatabaseOptions>(ConfigurationScope.Database, []);

        Assert.Contains("Database:Provider", error);
        Assert.Contains("ConnectionStrings:Default", error);
    }

    [Fact]
    public void Jwt_debil_falla_nombrando_la_clave()
    {
        var error = Validate<JwtOptions>(ConfigurationScope.Jwt, new Dictionary<string, string?>
        {
            ["Jwt:SigningKey"] = "corta",
            ["Jwt:Issuer"] = "Facturacion.Api",
            ["Jwt:Audience"] = "Facturacion.Client",
        });

        Assert.Contains("Jwt:SigningKey", error);
    }

    [Fact]
    public void Invitacion_sin_url_https_falla_nombrando_la_clave()
    {
        var error = Validate<InvitationOptions>(ConfigurationScope.Invitations, new Dictionary<string, string?>
        {
            ["Invitations:AcceptUrl"] = "http://inseguro.example.test/aceptar"
        });

        Assert.Contains("Invitations:AcceptUrl", error);
    }

    [Fact]
    public void Outbox_con_lote_cero_falla_nombrando_la_clave()
    {
        var error = Validate<OutboxOptions>(ConfigurationScope.Outbox, new Dictionary<string, string?>
        {
            ["Outbox:BatchSize"] = "0"
        });

        Assert.Contains("Outbox:BatchSize", error);
    }

    // ---------- Opcional: ausente se apaga, a medias es error ----------

    [Fact]
    public void Stripe_ausente_no_impide_arrancar()
    {
        using var provider = Build(ConfigurationScope.None, []);

        // Skip: el validador opcional no falla, solo avisa.
        Assert.NotNull(provider.GetRequiredService<IOptions<StripeOptions>>().Value);
    }

    [Fact]
    public void Stripe_con_una_sola_clave_es_error_de_despliegue()
    {
        var error = Validate<StripeOptions>(ConfigurationScope.None, new Dictionary<string, string?>
        {
            ["Stripe:SecretKey"] = "sk_test_123"
        });

        Assert.Contains("Stripe", error);
        Assert.Contains("WebhookSecret", error);
    }

    [Fact]
    public void Email_ausente_no_impide_arrancar()
    {
        using var provider = Build(ConfigurationScope.None, []);

        Assert.NotNull(provider.GetRequiredService<IOptions<EmailOptions>>().Value);
    }

    [Fact]
    public void Email_a_medias_falla()
    {
        var error = Validate<EmailOptions>(ConfigurationScope.None, new Dictionary<string, string?>
        {
            ["Email:FromAddress"] = "no-reply@example.test",
            ["Email:Smtp:Host"] = ""
        });

        Assert.Contains("Email:Smtp:Host", error);
    }

    // ---------- Utilidades ----------

    private static ServiceProvider Build(ConfigurationScope scope, Dictionary<string, string?> settings)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(
            new ConfigurationBuilder().AddInMemoryCollection(settings).Build());
        services.AddFacturacionOptions(scope);
        return services.BuildServiceProvider();
    }

    private static string Validate<TOptions>(ConfigurationScope scope, Dictionary<string, string?> settings)
        where TOptions : class
    {
        using var provider = Build(scope, settings);
        var error = Assert.Throws<OptionsValidationException>(() =>
            _ = provider.GetRequiredService<IOptions<TOptions>>().Value);
        return error.Message;
    }
}