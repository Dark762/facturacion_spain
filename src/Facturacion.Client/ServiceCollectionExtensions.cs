using Microsoft.Extensions.DependencyInjection;

namespace Facturacion.Client;

public static class ServiceCollectionExtensions
{
    /// <summary>Registra los clientes compartidos sin acoplarlos a un host o plataforma.</summary>
    public static IServiceCollection AddFacturacionApiClient(this IServiceCollection services, Uri baseAddress)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(baseAddress);
        if (!baseAddress.IsAbsoluteUri || (baseAddress.Scheme != Uri.UriSchemeHttp && baseAddress.Scheme != Uri.UriSchemeHttps))
            throw new ArgumentException("La URL de la API debe ser HTTP(S) absoluta.", nameof(baseAddress));

        services.AddHttpClient<ApiClient>(http => http.BaseAddress = baseAddress);
        services.AddTransient<ProductsApiClient>();
        services.AddTransient<CustomersApiClient>();
        services.AddTransient<InvoicesApiClient>();
        services.AddTransient<SaasApiClient>();
        services.AddTransient<AuthenticationApiClient>();
        return services;
    }
}
