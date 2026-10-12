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
    public static IServiceCollection AddSaasSelfService(this IServiceCollection services, IConfiguration config)
    {
        var section = config.GetSection("Stripe");
        // Solo se valida si la seccion existe: sin configuracion la funcion
        // queda inactiva (el webhook rechaza todo al no poder verificar la
        // firma) en lugar de impedir que arranque el resto de la API.
        if (section.Exists())
        {
            var stripe = section.Get<StripeOptions>()
                ?? throw new InvalidOperationException("Falta configuración Stripe.");

            if (string.IsNullOrWhiteSpace(stripe.WebhookSecret) ||
                string.IsNullOrWhiteSpace(stripe.SecretKey))
                throw new InvalidOperationException(
                    "Configuración Stripe inválida. Usa Stripe:SecretKey y Stripe:WebhookSecret.");
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
        services.AddScoped<StripeEventProcessor>();
        services.AddScoped<IProvisioningService, ProvisioningService>();

        return services;
    }
}