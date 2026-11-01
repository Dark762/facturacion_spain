namespace Facturacion.Domain.Enums;

/// <summary>Estado persistido de la suscripción. Stripe utiliza valores en minúsculas.</summary>
public enum SubscriptionStatus
{
    Active,
    Trialing,
    PastDue,
    Canceled,
    Unpaid,
    Incomplete,
    IncompleteExpired,
    Paused,
    Unknown
}

public static class SubscriptionStatusExtensions
{
    public static string ToStorageValue(this SubscriptionStatus status) => status switch
    {
        SubscriptionStatus.PastDue => "past_due",
        SubscriptionStatus.IncompleteExpired => "incomplete_expired",
        _ => status.ToString().ToLowerInvariant()
    };
}
