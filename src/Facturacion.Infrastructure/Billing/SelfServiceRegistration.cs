using Facturacion.Application.Billing;
using Facturacion.Application.Provisioning;
using Facturacion.Infrastructure.Persistence.Providers;
using Facturacion.Infrastructure.Provisioning;
using Facturacion.Payments.Stripe;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Facturacion.Infrastructure.Billing;

public static class SelfServiceRegistration
{
    public static IServiceCollection AddSaasSelfService(this IServiceCollection services, IConfiguration config, bool includeWebhookProcessing = true)
    {
        var section = config.GetSection("Stripe");
        var stripe = section.Get<StripeOptions>() ?? new StripeOptions();

        // Solo el par de credenciales marca Stripe como configurado. Tener las
        // URLs de retorno sin claves es el estado normal en desarrollo local y
        // no debe impedir que arranque el resto de la API.
        //
        // Si no hay ninguna clave, Stripe queda inactivo: el webhook rechaza
        // cada llamada al no poder verificar la firma. Si hay una pero falta la
        // otra, es un error de despliegue y si se avisa.
        var hasKeys = !string.IsNullOrWhiteSpace(stripe.SecretKey)
                      || !string.IsNullOrWhiteSpace(stripe.WebhookSecret);

        if (hasKeys)
        {
            if (string.IsNullOrWhiteSpace(stripe.WebhookSecret) ||
                string.IsNullOrWhiteSpace(stripe.SecretKey))
                throw new InvalidOperationException(
                    "Configuración Stripe incompleta: define ambos, Stripe:SecretKey y Stripe:WebhookSecret. " +
                    "Si no vas a usar Stripe, deja ambos vacíos.");
            if (!Uri.TryCreate(stripe.SuccessUrl, UriKind.Absolute, out _) ||
                !Uri.TryCreate(stripe.CancelUrl, UriKind.Absolute, out _))
                throw new InvalidOperationException(
                    "Configuración Stripe inválida. SuccessUrl y CancelUrl deben ser URI absolutas.");
        }

        services.Configure<StripeOptions>(config.GetSection("Stripe"));

        // Comparte resolutor y cadena con BillingDbContext y AuthDbContext.
        services.AddConfiguredDatabaseContext<SelfServiceDbContext>(config);

        services.AddHttpClient<IPaymentGateway, StripeCheckoutGateway>();
        services.AddScoped<SaasRegistrationService>();
        // El procesamiento de webhooks y el aprovisionamiento solo se habilitan en la API.
        // La web pública necesita únicamente registro, planes y creación de checkout.
        if (includeWebhookProcessing)
        {
            services.AddScoped<StripeEventProcessor>();
            services.AddScoped<IProvisioningService, ProvisioningService>();
        }

        return services;
    }
}
