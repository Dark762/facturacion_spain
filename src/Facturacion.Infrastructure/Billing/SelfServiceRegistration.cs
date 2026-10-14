using Facturacion.Application.Billing;
using Facturacion.Application.Provisioning;
using Facturacion.Infrastructure.Persistence.Providers;
using Facturacion.Infrastructure.Provisioning;
using Facturacion.Payments.Stripe;
using Microsoft.Extensions.DependencyInjection;

namespace Facturacion.Infrastructure.Billing;

public static class SelfServiceRegistration
{
    /// <summary>
    /// Registra el alta self-service y la pasarela de pago.
    ///
    /// Stripe es opcional y ya no se valida aqui: lo hace
    /// StripeOptionsValidator, que distingue "sin claves" (valido, el alta
    /// queda desactivada) de "con una sola clave" (despliegue a medias, error).
    /// Por eso el registro ya no necesita IConfiguration.
    /// </summary>
    /// <param name="includeWebhookProcessing">
    /// False en los hosts que no exponen el webhook. La web publica solo
    /// necesita registro, planes y creacion de checkout.
    /// </param>
    public static IServiceCollection AddSaasSelfService(
        this IServiceCollection services, bool includeWebhookProcessing = true)
    {
        // Comparte resolutor y cadena con BillingDbContext y AuthDbContext.
        services.AddConfiguredDatabaseContext<SelfServiceDbContext>();

        services.AddHttpClient<IPaymentGateway, StripeCheckoutGateway>();
        services.AddScoped<SaasRegistrationService>();

        if (includeWebhookProcessing)
        {
            services.AddScoped<StripeEventProcessor>();
            services.AddScoped<IProvisioningService, ProvisioningService>();
        }

        return services;
    }
}