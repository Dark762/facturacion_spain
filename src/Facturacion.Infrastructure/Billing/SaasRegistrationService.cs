using System.Security.Cryptography;
using System.Text;
using Facturacion.Application.Billing;
using Facturacion.Application.Common.Results;
using Facturacion.Domain.Enums;
using Facturacion.Application.Provisioning;
using Facturacion.Configuration.Options;
using Facturacion.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Facturacion.Payments.Stripe;

namespace Facturacion.Infrastructure.Billing;
public sealed class SaasRegistrationService : ISaasRegistrationService
{
    private readonly SelfServiceDbContext _db;
    private readonly UserManager<ApplicationUser> _users;
    private readonly IRegistrationEmailSender _emails;
    private readonly IPaymentGateway _gateway;
    private readonly IProvisioningService _provisioning;
    private readonly StripeOptions _stripe;
    public SaasRegistrationService(SelfServiceDbContext db,
        UserManager<ApplicationUser> users,
        IRegistrationEmailSender emails,
        IPaymentGateway gateway,
        IOptions<StripeOptions> stripe,
        IProvisioningService provisioning)
    {
        _db = db;
        _users = users;
        _emails = emails;
        _gateway = gateway;
        _stripe = stripe.Value;
        _provisioning = provisioning;
    }

    public async Task<RegistrationResponse> RegisterAsync(RegisterSaasRequest r, CancellationToken ct)
    {
        // Paso 1: solo se exigen los datos de acceso. Los datos fiscales se
        // validan despues en CompleteCompanyAsync (paso 3).
        if (string.IsNullOrWhiteSpace(r.Email) || string.IsNullOrWhiteSpace(r.Password) ||
            string.IsNullOrWhiteSpace(r.DisplayName))
            throw new BusinessException("REGISTRATION_REQUIRED_FIELDS", "Faltan datos obligatorios de registro.", 400);

        var email = r.Email.Trim();

        if (await _users.FindByEmailAsync(email) is not null)
            throw new BusinessException("REGISTRATION_EMAIL_EXISTS", "Este correo ya está registrado. Si comenzaste el registro y no lo completaste, utiliza «Continuar registro pendiente». Si tu cuenta ya está activa, inicia sesión.", 409);

        var user = new ApplicationUser { Email = email, UserName = email, DisplayName = r.DisplayName, IsActive = true, EmailConfirmed = false };

        var result = await _users.CreateAsync(user, r.Password);

        if (!result.Succeeded) throw new BusinessException("REGISTRATION_INVALID_PASSWORD", string.Join("; ", result.Errors.Select(e => e.Description)), 400);

        var intent = new RegistrationIntent
        {
            UserId = user.Id,
            TenantName = r.DisplayName.Trim(),
            Nif = "",
            LegalName = "",
            Address = "",
            PostalCode = "",
            City = "",
            Province = ""
        };
        _db.RegistrationIntents.Add(intent);

        await _db.SaveChangesAsync(ct);

        // Código numérico TOTP de Identity, asociado al sello de seguridad del usuario.
        // Expira automáticamente en pocos minutos y no se persiste en claro.
        var token = await _users.GenerateTwoFactorTokenAsync(user, TokenOptions.DefaultEmailProvider);

        await _emails.SendConfirmationAsync(email, intent.Id, token, ct);

        return new RegistrationResponse(intent.Id, "Comprueba tu correo para verificar la cuenta.");
    }

    public async Task<RegistrationResponse> ResumeAsync(ResumeRegistrationRequest request, CancellationToken ct)
    {
        const string genericMessage = "Si el correo corresponde a un registro pendiente, recibirás un código de verificación.";
        var email = request.Email?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(email))
            return new RegistrationResponse(Guid.NewGuid(), genericMessage);

        var user = await _users.FindByEmailAsync(email);
        if (user is null || !user.IsActive || await _users.IsLockedOutAsync(user))
            return new RegistrationResponse(Guid.NewGuid(), genericMessage);

        var intent = await _db.RegistrationIntents.SingleOrDefaultAsync(x => x.UserId == user.Id, ct);
        if (intent is null || intent.Status.Is(RegistrationStatus.Active) ||
            intent.TenantId is not null && await _db.SubscriptionStates
                .AnyAsync(x => x.TenantId == intent.TenantId &&
                    x.Status == SubscriptionStatus.Active.ToStorageValue(), ct))
            return new RegistrationResponse(Guid.NewGuid(), genericMessage);

        var token = await _users.GenerateTwoFactorTokenAsync(user, TokenOptions.DefaultEmailProvider);

        await _emails.SendConfirmationAsync(email, intent.Id, token, ct);
        return new RegistrationResponse(intent.Id, genericMessage);
    }

    public async Task<RegistrationProgress> VerifyAndResumeAsync(Guid id, string token, string newPassword, CancellationToken ct)
    {
        var intent = await _db.RegistrationIntents.SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new BusinessException("REGISTRATION_NOT_FOUND", "Registro inexistente.", 404);

        var user = await _users.FindByIdAsync(intent.UserId)
            ?? throw new BusinessException("REGISTRATION_USER_NOT_FOUND", "Usuario inexistente.", 404);

        if (intent.Status.Is(RegistrationStatus.Active) || !user.IsActive ||
            await _users.IsLockedOutAsync(user) ||
            intent.TenantId is not null && await _db.SubscriptionStates
                .AnyAsync(x => x.TenantId == intent.TenantId &&
                    x.Status == SubscriptionStatus.Active.ToStorageValue(), ct))
            throw new BusinessException("REGISTRATION_NOT_RECOVERABLE", "Este registro no se puede recuperar.", 409);

        var valid = token is { Length: RegistrationRules.VerificationCodeLength } &&
            token.All(char.IsAsciiDigit) &&
            await _users.VerifyTwoFactorTokenAsync(user, TokenOptions.DefaultEmailProvider, token);

        if (!valid)
        {
            await _users.AccessFailedAsync(user);
            throw new BusinessException("REGISTRATION_INVALID_CODE", "Código inválido o caducado.", 400);
        }
        // El código autoriza establecer una nueva contraseña sin conocer la anterior.
        // Identity aplica las mismas reglas de seguridad que en el alta.
        var passwordErrors = new List<IdentityError>();
        foreach (var validator in _users.PasswordValidators)
        {
            var validation = await validator.ValidateAsync(_users, user, newPassword);
            if (!validation.Succeeded)
                passwordErrors.AddRange(validation.Errors);
        }
        if (passwordErrors.Count > 0)
            throw new BusinessException("REGISTRATION_INVALID_PASSWORD", string.Join("; ", passwordErrors.Select(x => x.Description)), 400);

        var resetToken = await _users.GeneratePasswordResetTokenAsync(user);

        var reset = await _users.ResetPasswordAsync(user, resetToken, newPassword);

        if (!reset.Succeeded)
            throw new BusinessException("REGISTRATION_PASSWORD_UPDATE_FAILED", "No se pudo completar la operación. Inténtalo de nuevo.", 500);

        if (!user.EmailConfirmed)
        {
            user.EmailConfirmed = true;
            var updated = await _users.UpdateAsync(user);
            if (!updated.Succeeded) throw new BusinessException("REGISTRATION_EMAIL_UPDATE_FAILED", "No se pudo completar la verificación. Inténtalo de nuevo.", 500);
        }

        await _users.ResetAccessFailedCountAsync(user);

        var secret = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

        intent.CheckoutTokenHash = Hash(secret);
        // El registro recuperado puede estar en CheckoutCreated o Provisioning:
        // conservar su estado y los IDs para evitar un nuevo aprovisionamiento.
        if (intent.Status.Is(RegistrationStatus.PendingEmail))
            intent.Status = RegistrationStatus.EmailVerified.ToStorageValue();

        await _db.SaveChangesAsync(ct);

        return new RegistrationProgress(secret,
            !string.IsNullOrWhiteSpace(intent.LegalName) && !string.IsNullOrWhiteSpace(intent.Nif) &&
            !string.IsNullOrWhiteSpace(intent.Address) && !string.IsNullOrWhiteSpace(intent.PostalCode) &&
            !string.IsNullOrWhiteSpace(intent.City) && !string.IsNullOrWhiteSpace(intent.Province),
            user.DisplayName ?? "", intent.HolderType, intent.TradeName, intent.LegalName,
            intent.Nif, intent.Address, intent.PostalCode, intent.City, intent.Province);
    }

    public async Task<string> VerifyEmailAsync(Guid id, string token, CancellationToken ct)
    {
        var intent = await _db.RegistrationIntents.SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new BusinessException("REGISTRATION_NOT_FOUND", "Registro inexistente.", 404);
        var user = await _users.FindByIdAsync(intent.UserId) ?? throw new BusinessException("REGISTRATION_USER_NOT_FOUND", "Usuario inexistente.", 404);
        // El registro solo puede verificarse una vez. Impide reutilizar el código.
        if (!intent.Status.Is(RegistrationStatus.PendingEmail) || user.EmailConfirmed ||
            await _users.IsLockedOutAsync(user))
            throw new BusinessException("REGISTRATION_INVALID_CODE", "Código inválido o caducado.", 400);

        var valid = token is { Length: RegistrationRules.VerificationCodeLength } &&
                    token.All(char.IsAsciiDigit) &&
                    await _users.VerifyTwoFactorTokenAsync(
                        user, TokenOptions.DefaultEmailProvider, token);
        if (!valid)
        {
            // Usa el bloqueo de Identity para limitar los intentos fallidos.
            var failed = await _users.AccessFailedAsync(user);
            if (!failed.Succeeded)
                throw new BusinessException("REGISTRATION_VERIFICATION_FAILED", "No se pudo completar la verificación. Inténtalo de nuevo.", 500);
            throw new BusinessException("REGISTRATION_INVALID_CODE", "Código inválido o caducado.", 400);
        }

        user.EmailConfirmed = true;
        var updated = await _users.UpdateAsync(user);
        if (!updated.Succeeded)
            throw new BusinessException("REGISTRATION_EMAIL_CONFIRMATION_FAILED", "No se pudo completar la verificación. Inténtalo de nuevo.", 500);
        await _users.ResetAccessFailedCountAsync(user);
        var secret = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        intent.CheckoutTokenHash = Hash(secret);
        intent.Status = RegistrationStatus.EmailVerified.ToStorageValue();
        await _db.SaveChangesAsync(ct);
        return secret; // Solo entregar tras verificar el token del correo; nunca registrar en logs.
    }

    public async Task CompleteCompanyAsync(Guid id, string checkoutToken, CompleteCompanyRequest request, CancellationToken ct)
    {
        RegistrationValidation.ThrowIfInvalidCompany(request);
        var intent = await _db.RegistrationIntents.SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new BusinessException("REGISTRATION_NOT_FOUND", "Registro inexistente.", 404);

        if (!intent.Status.Is(RegistrationStatus.EmailVerified) || intent.CheckoutTokenHash is null ||
            !CryptographicOperations.FixedTimeEquals(
                Convert.FromHexString(intent.CheckoutTokenHash),
                Convert.FromHexString(Hash(checkoutToken))))
            throw new BusinessException("REGISTRATION_NOT_VERIFIED", "Registro no verificado o sesión caducada.", 403);

        intent.TenantName = string.IsNullOrWhiteSpace(request.TradeName) ? request.LegalName.Trim() : request.TradeName.Trim();

        intent.HolderType = request.HolderType;
        intent.TradeName = request.TradeName?.Trim() ?? "";
        intent.LegalName = request.LegalName.Trim();
        intent.Nif = request.Nif.Trim().ToUpperInvariant();
        intent.Address = request.Address.Trim();
        intent.PostalCode = request.PostalCode.Trim();
        intent.City = request.City.Trim();
        intent.Province = request.Province.Trim();
        await _db.SaveChangesAsync(ct);
    }

    public async Task<CheckoutResponse> CheckoutAsync(Guid id, string checkoutToken, string planCode, CancellationToken ct)
    {
        var intent = await _db.RegistrationIntents.SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new BusinessException("REGISTRATION_NOT_FOUND", "Registro inexistente.", 404);

        // El estado Provisioning permite reanudar un alta gratuita interrumpida.
        // El token de recuperación sigue siendo obligatorio en todos los casos.
        if ((!intent.Status.Is(RegistrationStatus.EmailVerified) &&
             !intent.Status.Is(RegistrationStatus.Provisioning)) ||
            intent.CheckoutTokenHash is null ||
            !CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(intent.CheckoutTokenHash),
                Encoding.UTF8.GetBytes(Hash(checkoutToken))))
            throw new BusinessException("REGISTRATION_NOT_VERIFIED", "Registro no verificado o sesión caducada.", 403);

        if (!RegistrationValues.IsValidFiscalHolder(intent.HolderType) || string.IsNullOrWhiteSpace(intent.LegalName) || string.IsNullOrWhiteSpace(intent.Nif) ||
            string.IsNullOrWhiteSpace(intent.Address) || string.IsNullOrWhiteSpace(intent.PostalCode) ||
            string.IsNullOrWhiteSpace(intent.City) || string.IsNullOrWhiteSpace(intent.Province))
            throw new BusinessException("REGISTRATION_FISCAL_DATA_REQUIRED", "Completa los datos fiscales antes de contratar.", 400);

        if (intent.CheckoutSessionId is not null)
            throw new BusinessException("REGISTRATION_CHECKOUT_EXISTS", "Ya existe una sesión de pago. Consulta su estado antes de crear otra.", 409);

        var plan = await _db.SubscriptionPlans.SingleOrDefaultAsync(x => x.Code == planCode && x.IsActive, ct)
            ?? throw new BusinessException("REGISTRATION_PLAN_NOT_FOUND", "Plan no disponible.", 404);

        if (!SubscriptionPlanCodeExtensions.TryFromCode(plan.Code, out var selectedPlan))
            throw new BusinessException("REGISTRATION_PLAN_INVALID", "El plan seleccionado no está disponible.", 400);

        if (intent.Status.Is(RegistrationStatus.Provisioning) &&
            selectedPlan != SubscriptionPlanCode.GRATIS)
            throw new BusinessException("REGISTRATION_PROVISIONING_PLAN_CONFLICT", "Solo se puede reanudar el aprovisionamiento del plan gratuito.", 409);

        if (selectedPlan == SubscriptionPlanCode.GRATIS)
        {
            if (plan.MonthlyPrice != 0)
                throw new BusinessException("REGISTRATION_FREE_PLAN_MISCONFIGURED", "No se puede activar este plan en este momento. Inténtalo más tarde.", 503);

            var result = await _provisioning.ProvisionAsync(new ProvisioningRequest(
                intent.Id, intent.UserId, intent.TenantName, intent.LegalName,
                intent.Nif, intent.Address, intent.PostalCode, intent.City, intent.Province), ct);

            var state = await _db.SubscriptionStates.SingleOrDefaultAsync(x => x.TenantId == result.TenantId, ct);
            if (state is null)
            {
                _db.SubscriptionStates.Add(new SubscriptionState
                {
                    TenantId = result.TenantId,
                    StripeCustomerId = null,
                    StripeSubscriptionId = null,
                    PlanCode = plan.Code,
                    Status = SubscriptionStatus.Active.ToStorageValue()
                });
            }

            intent.PlanCode = plan.Code;

            intent.TenantId = result.TenantId;

            intent.Status = RegistrationStatus.Active.ToStorageValue();

            await _db.SaveChangesAsync(ct);

            return new CheckoutResponse("", true);
        }

        if (string.IsNullOrWhiteSpace(plan.StripePriceId))
            throw new BusinessException("REGISTRATION_PAYMENT_NOT_CONFIGURED", "El pago no está disponible en este momento. Inténtalo más tarde.", 503);

        var user = await _users.FindByIdAsync(intent.UserId) ?? throw new BusinessException("REGISTRATION_USER_NOT_FOUND", "Usuario inexistente.", 404);

        var (sessionId, url) = await _gateway.CreateSubscriptionCheckoutAsync(plan.StripePriceId, user.Email!,
            id.ToString("D"), _stripe.SuccessUrl, _stripe.CancelUrl, ct);

        intent.PlanCode = plan.Code;
        intent.CheckoutSessionId = sessionId;
        intent.Status = RegistrationStatus.CheckoutCreated.ToStorageValue();

        await _db.SaveChangesAsync(ct);

        return new CheckoutResponse(url);
    }
    private static string Hash(string s) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(s)));
}
