using Microsoft.AspNetCore.Antiforgery;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Facturacion.Integration.Test;

/// <summary>
/// UseAntiforgery() necesita que alguien haya registrado Antiforgery. En los
/// hosts Blazor eso lo hace AddRazorComponents, de forma implicita: nadie escribe
/// AddAntiforgery() y aun asi funciona.
///
/// Esa relacion es fragil: si AddRazorComponents falta o cambia, el error sale
/// al arrancar y no al compilar. Se fija aqui para que un test lo detecte.
/// </summary>
public sealed class AntiforgeryCompositionTests
{
    [Fact]
    public void AddRazorComponents_aporta_Antiforgery_para_UseAntiforgery()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddRazorComponents().AddInteractiveServerComponents();

        using var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetService<IAntiforgery>());
    }

    [Fact]
    public void Un_contenedor_sin_registrar_Razor_Components_no_tiene_Antiforgery()
    {
        // Contraprueba: el servicio no aparece por sorpresa en cualquier sitio.
        var services = new ServiceCollection();
        services.AddLogging();

        using var provider = services.BuildServiceProvider();

        Assert.Null(provider.GetService<IAntiforgery>());
    }
}