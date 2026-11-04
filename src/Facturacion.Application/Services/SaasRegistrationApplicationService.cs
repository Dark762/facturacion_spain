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

    public async Task<Result<RegistrationResponse>> RegisterAsync(RegisterSaasRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _registration.RegisterAsync(request, cancellationToken);
            return Result<RegistrationResponse>.Success(response);
        }
        catch (ArgumentException ex)
        {
            // Identity agrupa los errores de contraseña con "; ".
            // Traducir cada regla y asociarla al campo Password por separado.
            var details = ex.Message.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            var passwordErrors = details
                .Where(message => message.StartsWith("Passwords ", StringComparison.OrdinalIgnoreCase))
                .Select(TranslatePasswordError)
                .Distinct()
                .ToArray();

            if (passwordErrors.Length > 0)
                return Result<RegistrationResponse>.Failure(passwordErrors.Select(message => ApplicationErrors.Invalid("PASSWORD_INVALID", message, "Password")).ToArray());

            return Result<RegistrationResponse>.Failure(ApplicationErrors.Invalid("REGISTRATION_INVALID", ex.Message, "request"));
        }
        catch (InvalidOperationException ex)
        {
            return Result<RegistrationResponse>.Failure(ApplicationErrors.Invalid("REGISTRATION_INVALID", ex.Message, "request"));
        }
    }

    public async Task<Result<RegistrationResponse>> ResumeAsync(ResumeRegistrationRequest request, CancellationToken ct)
    {
        try
        {
            return Result<RegistrationResponse>.Success(await _registration.ResumeAsync(request, ct));
        }
        catch (InvalidOperationException)
        {
            return Result<RegistrationResponse>.Failure(ApplicationErrors.Invalid("RESUME_FAILED", "No se ha podido recuperar el registro.", "request"));
        }
    }

    public async Task<Result<RegistrationProgress>> VerifyAndResumeAsync(Guid id, string token, string newPassword, CancellationToken ct)
    {
        try
        {
            return Result<RegistrationProgress>.Success(await _registration.VerifyAndResumeAsync(id, token, newPassword, ct));
        }
        catch (KeyNotFoundException)
        {
            return Result<RegistrationProgress>.Failure(ApplicationErrors.NotFound("Registro"));
        }
        catch (ArgumentException ex)
        {
            return Result<RegistrationProgress>.Failure(ApplicationErrors.Invalid("PASSWORD_INVALID", TranslatePasswordError(ex.Message), "newPassword"));
        }
        catch (UnauthorizedAccessException)
        {
            return Result<RegistrationProgress>.Failure(ApplicationErrors.Invalid("INVALID_TOKEN", "Código inválido o caducado.", "token"));
        }
    }

    public async Task<Result<string>> VerifyEmailAsync(
        Guid registrationId, string token, CancellationToken cancellationToken)
    {
        try
        {
            var checkoutToken = await _registration.VerifyEmailAsync(registrationId, token, cancellationToken);

            return Result<string>.Success(checkoutToken);
        }
        catch (KeyNotFoundException)
        {
            return Result<string>.Failure(ApplicationErrors.NotFound("Registro"));
        }
        catch (UnauthorizedAccessException)
        {
            return Result<string>.Failure(ApplicationErrors.Invalid("INVALID_TOKEN", "Token inválido o caducado.", "token"));
        }
    }

    public async Task<Result> CompleteCompanyAsync(
        Guid registrationId, string checkoutToken, CompleteCompanyRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            await _registration.CompleteCompanyAsync(registrationId, checkoutToken, request, cancellationToken);
            return Result.Success();
        }
        catch (KeyNotFoundException)
        {
            return Result.Failure(ApplicationErrors.NotFound("Registro"));
        }
        catch (UnauthorizedAccessException)
        {
            return Result.Failure(ApplicationErrors.Invalid("REGISTRATION_NOT_VERIFIED", "Registro no verificado.", "checkoutToken"));
        }
        catch (RegistrationValidationException ex)
        {
            return Result.Failure(ex.Errors.Select(kv => ApplicationErrors.Invalid(kv.Key, string.Join("; ", kv.Value), kv.Key)).ToArray());
        }
    }

    public async Task<Result<CheckoutResponse>> CheckoutAsync(Guid registrationId, string checkoutToken, string planCode,CancellationToken cancellationToken)
    {
        try
        {
            var response = await _registration.CheckoutAsync(registrationId, checkoutToken, planCode, cancellationToken);

            return Result<CheckoutResponse>.Success(response);
        }
        catch (KeyNotFoundException)
        {
            return Result<CheckoutResponse>.Failure(ApplicationErrors.NotFound("Registro"));
        }
        catch (UnauthorizedAccessException)
        {
            return Result<CheckoutResponse>.Failure(ApplicationErrors.Invalid("REGISTRATION_NOT_VERIFIED", "Registro no verificado.", "checkoutToken"));
        }
        catch (InvalidOperationException ex)
        {
            return Result<CheckoutResponse>.Failure(ApplicationErrors.Invalid("CHECKOUT_INVALID", ex.Message, "planCode"));
        }
    }
    private static string TranslatePasswordError(string message)
    {
        if (message.StartsWith("Passwords must be at least ", StringComparison.OrdinalIgnoreCase))
            return "La contraseña debe tener al menos " +
                message["Passwords must be at least ".Length..].Replace(" characters.", " caracteres.");

        if (message.Contains("non alphanumeric", StringComparison.OrdinalIgnoreCase))
            return "La contraseña debe incluir al menos un carácter especial.";

        if (message.Contains("at least one digit", StringComparison.OrdinalIgnoreCase))
            return "La contraseña debe incluir al menos un número.";

        if (message.Contains("at least one uppercase", StringComparison.OrdinalIgnoreCase))
            return "La contraseña debe incluir al menos una letra mayúscula.";

        if (message.Contains("at least one lowercase", StringComparison.OrdinalIgnoreCase))
            return "La contraseña debe incluir al menos una letra minúscula.";

        if (message.Contains("unique chars", StringComparison.OrdinalIgnoreCase))
            return "La contraseña debe contener más caracteres diferentes.";

        return "La contraseña no cumple los requisitos de seguridad.";
    }
}
