namespace Facturacion.Application.Billing;

public interface ISaasRegistrationService
{
    Task<RegistrationResponse> RegisterAsync(RegisterSaasRequest request, CancellationToken cancellationToken);
    Task<RegistrationResponse> ResumeAsync(ResumeRegistrationRequest request, CancellationToken cancellationToken);
    Task<RegistrationProgress> VerifyAndResumeAsync(Guid registrationId, string token, string newPassword, CancellationToken cancellationToken);
    Task<string> VerifyEmailAsync(Guid registrationId, string token, CancellationToken cancellationToken);
    Task CompleteCompanyAsync(Guid registrationId, string checkoutToken, CompleteCompanyRequest request, CancellationToken cancellationToken);
    Task<CheckoutResponse> CheckoutAsync(Guid registrationId, string checkoutToken, string planCode, CancellationToken cancellationToken);
}
