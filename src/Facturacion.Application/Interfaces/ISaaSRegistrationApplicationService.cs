using Facturacion.Application.Billing;
using Facturacion.Application.Common.Results;

namespace Facturacion.Application.Interfaces;

public interface ISaaSRegistrationApplicationService
{
    Task<Result<RegistrationResponse>> RegisterAsync(
        RegisterSaasRequest request, CancellationToken cancellationToken);

    Task<Result<string>> VerifyEmailAsync(
        Guid registrationId, string token, CancellationToken cancellationToken);

    Task<Result> CompleteCompanyAsync(
        Guid registrationId, string checkoutToken, CompleteCompanyRequest request,
        CancellationToken cancellationToken);

    Task<Result<CheckoutResponse>> CheckoutAsync(
        Guid registrationId, string checkoutToken, string planCode,
        CancellationToken cancellationToken);
}
