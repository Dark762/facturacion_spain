namespace Facturacion.Application.Billing;

public enum FiscalHolderType { Individual, LegalEntity }
public enum RegistrationStatus { PendingEmail, EmailVerified, CheckoutCreated }
public enum RegistrationStep { Account = 0, EmailVerification = 1, CompanyDetails = 2, PlanSelection = 3 }

/// <summary>Valores de negocio compartidos por las capas de registro.</summary>
public static class RegistrationRules
{
    public const int MinimumPasswordLength = 12;
    public const int VerificationCodeLength = 6;
}

/// <summary>Conserva los valores de texto existentes en API y MySQL.</summary>
public static class RegistrationValues
{
    public static string ToStorageValue(this FiscalHolderType value) => value.ToString();
    public static string ToStorageValue(this RegistrationStatus value) => value.ToString();
    public static bool Is(this string? value, FiscalHolderType expected) =>
        string.Equals(value, expected.ToStorageValue(), StringComparison.Ordinal);
    public static bool Is(this string? value, RegistrationStatus expected) =>
        string.Equals(value, expected.ToStorageValue(), StringComparison.Ordinal);
    public static bool IsValidFiscalHolder(string? value) =>
        value.Is(FiscalHolderType.Individual) || value.Is(FiscalHolderType.LegalEntity);
}
