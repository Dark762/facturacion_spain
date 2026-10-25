namespace Facturacion.Application.Billing;

public interface ISaasRegistrationService
{
    Task<RegistrationResponse> RegisterAsync(RegisterSaasRequest request, CancellationToken cancellationToken);
    Task<string> VerifyEmailAsync(Guid registrationId, string token, CancellationToken cancellationToken);
    Task<CheckoutResponse> CheckoutAsync(Guid registrationId, string checkoutToken, string planCode, CancellationToken cancellationToken);
}
