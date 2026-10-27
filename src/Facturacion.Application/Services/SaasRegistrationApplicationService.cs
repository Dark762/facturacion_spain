using Facturacion.Application.Billing;
using Facturacion.Application.Common.Errors;
using Facturacion.Application.Common.Results;
using Facturacion.Application.Interfaces;

namespace Facturacion.Application.Services;

public sealed class SaasRegistrationApplicationService : ISaaSRegistrationApplicationService
{
    private readonly ISaasRegistrationService _registration;

    public SaasRegistrationApplicationService(ISaasRegistrationService registration)
    {
        _registration = registration;
    }

    public async Task<Result<RegistrationResponse>> RegisterAsync(
        RegisterSaasRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _registration.RegisterAsync(request, cancellationToken);
            return Result<RegistrationResponse>.Success(response);
        }
        catch (ArgumentException ex)
        {
            return Result<RegistrationResponse>.Failure(
                ApplicationErrors.Invalid("REGISTRATION_INVALID", ex.Message, "request"));
        }
        catch (InvalidOperationException ex)
        {
            return Result<RegistrationResponse>.Failure(
                ApplicationErrors.Invalid("REGISTRATION_INVALID", ex.Message, "request"));
        }
    }

    public async Task<Result<string>> VerifyEmailAsync(
        Guid registrationId, string token, CancellationToken cancellationToken)
    {
        try
        {
            var checkoutToken = await _registration.VerifyEmailAsync(
                registrationId, token, cancellationToken);
            return Result<string>.Success(checkoutToken);
        }
        catch (KeyNotFoundException)
        {
            return Result<string>.Failure(
                ApplicationErrors.NotFound("Registro"));
        }
        catch (UnauthorizedAccessException)
        {
            return Result<string>.Failure(
                ApplicationErrors.Invalid("INVALID_TOKEN", "Token inválido o caducado.", "token"));
        }
    }

    public async Task<Result> CompleteCompanyAsync(
        Guid registrationId, string checkoutToken, CompleteCompanyRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            await _registration.CompleteCompanyAsync(
                registrationId, checkoutToken, request, cancellationToken);
            return Result.Success();
        }
        catch (KeyNotFoundException)
        {
            return Result.Failure(ApplicationErrors.NotFound("Registro"));
        }
        catch (UnauthorizedAccessException)
        {
            return Result.Failure(
                ApplicationErrors.Invalid("REGISTRATION_NOT_VERIFIED", "Registro no verificado.", "checkoutToken"));
        }
        catch (RegistrationValidationException ex)
        {
            return Result.Failure(
                ex.Errors.Select(kv =>
                    ApplicationErrors.Invalid(kv.Key, string.Join("; ", kv.Value), kv.Key)).ToArray());
        }
    }

    public async Task<Result<CheckoutResponse>> CheckoutAsync(
        Guid registrationId, string checkoutToken, string planCode,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _registration.CheckoutAsync(
                registrationId, checkoutToken, planCode, cancellationToken);
            return Result<CheckoutResponse>.Success(response);
        }
        catch (KeyNotFoundException)
        {
            return Result<CheckoutResponse>.Failure(
                ApplicationErrors.NotFound("Registro"));
        }
        catch (UnauthorizedAccessException)
        {
            return Result<CheckoutResponse>.Failure(
                ApplicationErrors.Invalid("REGISTRATION_NOT_VERIFIED", "Registro no verificado.", "checkoutToken"));
        }
        catch (InvalidOperationException ex)
        {
            return Result<CheckoutResponse>.Failure(
                ApplicationErrors.Invalid("CHECKOUT_INVALID", ex.Message, "planCode"));
        }
    }
}
