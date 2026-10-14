using Facturacion.Infrastructure.Identity;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Facturacion.Integration.Test;

/// <summary>
/// La web publica monta Identity sin esquema de sesion: solo registra cuentas y
/// emite tokens de verificacion. AddIdentityCore no arrastra Data Protection
/// (solo lo hace AddAuthentication), y DataProtectorTokenProvider lo necesita.
///
/// Si falta ese registro el contenedor revienta al construir, asi que se fija
/// aqui: es una dependencia implicita que no se ve leyendo el codigo.
/// </summary>
public sealed class PublicIdentityCompositionTests
{
    [Fact]
    public void La_cadena_de_Identity_sin_esquema_de_sesion_se_puede_resolver()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        // Esta es la composicion exacta de Facturacion.Web.Public/Program.cs.
        services.AddDataProtection();
        services.AddIdentityCore<ApplicationUser>()
            .AddDefaultTokenProviders();

        using var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetRequiredService<IDataProtectionProvider>());
        Assert.NotNull(provider.GetRequiredService<DataProtectorTokenProvider<ApplicationUser>>());
    }

    [Fact]
    public void Sin_AddDataProtection_la_cadena_no_se_puede_construir()
    {
        // Documenta por que el registro explicito es necesario y no superfluo.
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddIdentityCore<ApplicationUser>()
            .AddDefaultTokenProviders();

        using var provider = services.BuildServiceProvider();

        Assert.Throws<InvalidOperationException>(() =>
            provider.GetRequiredService<DataProtectorTokenProvider<ApplicationUser>>());
    }
}