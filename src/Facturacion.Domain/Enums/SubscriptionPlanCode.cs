namespace Facturacion.Domain.Enums;

/// <summary>Códigos de planes almacenados en SubscriptionPlans.Code.</summary>
public enum SubscriptionPlanCode
{
    ASESORIA,
    AUTONOMO,
    EMPRESA,
    GRATIS,
    NEGOCIO
}

public static class SubscriptionPlanCodeExtensions
{
    public static bool TryFromCode(string? code, out SubscriptionPlanCode plan) =>
        Enum.TryParse(code?.Trim(), ignoreCase: true, out plan)
        && Enum.IsDefined(plan);

    public static string ToStorageCode(this SubscriptionPlanCode plan) => plan.ToString();
}
