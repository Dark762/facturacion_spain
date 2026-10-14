namespace Facturacion.Configuration.Options;

/// <summary>
/// Pasarela de pago. Opcional: sin credenciales el alta self-service queda
/// desactivada, pero el resto de la aplicacion arranca.
/// </summary>
public sealed class StripeOptions
{
    public const string SectionName = "Stripe";

    public string SecretKey { get; set; } = "";
    public string WebhookSecret { get; set; } = "";
    public string SuccessUrl { get; set; } = "";
    public string CancelUrl { get; set; } = "";

    /// <summary>
    /// Solo hay una credencial: es un despliegue a medias, no una opcion
    /// valida. La distingue <see cref="IsConfigured"/>, que exige las dos.
    /// </summary>
    public bool HasAnyCredential =>
        !string.IsNullOrWhiteSpace(SecretKey) || !string.IsNullOrWhiteSpace(WebhookSecret);

    /// <summary>El componente esta operativo.</summary>
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(SecretKey) && !string.IsNullOrWhiteSpace(WebhookSecret);
}