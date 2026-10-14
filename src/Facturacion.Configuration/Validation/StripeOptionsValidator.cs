using Facturacion.Configuration.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Facturacion.Configuration.Validation;

/// <summary>
/// Stripe. Opcional, con una sutileza: no tener claves es una situacion
/// valida (desarrollo local, o un despliegue que aun no abre el alta), pero
/// tener solo una de las dos es un despliegue a medias que hay que señalar
/// alto y claro en vez de dejar el cobro medio activo.
/// </summary>
public sealed class StripeOptionsValidator(ILogger<StripeOptionsValidator> logger) : IValidateOptions<StripeOptions>
{
    public ValidateOptionsResult Validate(string? name, StripeOptions options)
    {
        if (!options.HasAnyCredential)
        {
            logger.LogWarning(
                "Stripe sin configurar: el alta self-service y el cobro quedaran desactivados. " +
                "Es la situacion esperada en desarrollo local.");
            return ValidateOptionsResult.Skip;
        }

        var failures = new List<string>();
        var prefix = StripeOptions.SectionName;

        if (string.IsNullOrWhiteSpace(options.SecretKey))
            failures.Add($"{prefix}:WebhookSecret esta definido pero falta {prefix}:SecretKey.");

        if (string.IsNullOrWhiteSpace(options.WebhookSecret))
            failures.Add($"{prefix}:SecretKey esta definido pero falta {prefix}:WebhookSecret.");

        foreach (var (key, value) in new[]
                 {
                     (nameof(StripeOptions.SuccessUrl), options.SuccessUrl),
                     (nameof(StripeOptions.CancelUrl), options.CancelUrl)
                 })
        {
            if (!Uri.TryCreate(value, UriKind.Absolute, out _))
                failures.Add($"{prefix}:{key} debe ser una URL absoluta.");
        }

        if (failures.Count > 0)
        {
            return ValidateOptionsResult.Fail(
                failures.Prepend(
                    $"{prefix} esta configurado a medias. Define ambas claves o ninguna: " +
                    "con una sola, el cobro queda activo y el webhook rechaza cada llamada."));
        }

        return ValidateOptionsResult.Success;
    }
}