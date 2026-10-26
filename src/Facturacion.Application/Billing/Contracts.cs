namespace Facturacion.Application.Billing;
public sealed record RegisterSaasRequest(string Email, string Password, string DisplayName,
    string TenantName = "", string Nif = "", string LegalName = "", string Address = "", string PostalCode = "",
    string City = "", string Province = "");
public sealed record CompleteCompanyRequest(string LegalName, string Nif, string Address, string PostalCode, string City, string Province);
public sealed record RegistrationResponse(Guid RegistrationId, string Message);
public sealed record CheckoutResponse(string Url);
public sealed record PlanResponse(string Code, string Name, decimal MonthlyPrice, string Currency, int MaxCompanies, int MaxUsers, int MaxInvoicesPerMonth, string Description);
public interface IPaymentGateway
{
    Task<(string SessionId, string Url)> CreateSubscriptionCheckoutAsync(
        string priceId, string customerEmail, string registrationId, string successUrl,
        string cancelUrl, CancellationToken ct);
}
