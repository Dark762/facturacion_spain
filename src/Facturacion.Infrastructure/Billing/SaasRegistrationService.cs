using System.Security.Cryptography;
using System.Text;
using Facturacion.Application.Billing;
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
    private readonly StripeOptions _stripe;
    public SaasRegistrationService(SelfServiceDbContext db, UserManager<ApplicationUser> users,
        IRegistrationEmailSender emails, IPaymentGateway gateway, IOptions<StripeOptions> stripe)
    { _db = db; _users = users; _emails = emails; _gateway = gateway; _stripe = stripe.Value; }

    public async Task<RegistrationResponse> RegisterAsync(RegisterSaasRequest r, CancellationToken ct)
    {
        // Paso 1: solo se exigen los datos de acceso. Los datos fiscales se
        // validan despues en CompleteCompanyAsync (paso 3).
        if (string.IsNullOrWhiteSpace(r.Email) || string.IsNullOrWhiteSpace(r.Password) ||
            string.IsNullOrWhiteSpace(r.DisplayName))
            throw new ArgumentException("Faltan datos obligatorios de registro.");
        var email = r.Email.Trim();
        if (await _users.FindByEmailAsync(email) is not null)
            throw new InvalidOperationException("No se puede completar el registro con el email ingresado.");

        var user = new ApplicationUser { Email = email, UserName = email, DisplayName = r.DisplayName, IsActive = true, EmailConfirmed = false };

        var result = await _users.CreateAsync(user, r.Password);

        if (!result.Succeeded) throw new ArgumentException(string.Join("; ", result.Errors.Select(e => e.Description)));

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

    public async Task<string> VerifyEmailAsync(Guid id, string token, CancellationToken ct)
    {
        var intent = await _db.RegistrationIntents.SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new KeyNotFoundException("Registro inexistente.");
        var user = await _users.FindByIdAsync(intent.UserId) ?? throw new KeyNotFoundException("Usuario inexistente.");
        // El registro solo puede verificarse una vez. Impide reutilizar el código.
        if (intent.Status != "PendingEmail" || user.EmailConfirmed ||
            await _users.IsLockedOutAsync(user))
            throw new UnauthorizedAccessException("Código inválido o caducado.");

        var valid = token is { Length: 6 } &&
                    token.All(char.IsAsciiDigit) &&
                    await _users.VerifyTwoFactorTokenAsync(
                        user, TokenOptions.DefaultEmailProvider, token);
        if (!valid)
        {
            // Usa el bloqueo de Identity para limitar los intentos fallidos.
            var failed = await _users.AccessFailedAsync(user);
            if (!failed.Succeeded)
                throw new InvalidOperationException("No se pudo registrar el intento de verificación.");
            throw new UnauthorizedAccessException("Código inválido o caducado.");
        }

        user.EmailConfirmed = true;
        var updated = await _users.UpdateAsync(user);
        if (!updated.Succeeded)
            throw new InvalidOperationException("No se pudo confirmar el correo.");
        await _users.ResetAccessFailedCountAsync(user);
        var secret = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        intent.CheckoutTokenHash = Hash(secret);
        intent.Status = "EmailVerified";
        await _db.SaveChangesAsync(ct);
        return secret; // Solo entregar tras verificar el token del correo; nunca registrar en logs.
    }

    public async Task CompleteCompanyAsync(Guid id, string checkoutToken, CompleteCompanyRequest request, CancellationToken ct)
    {
        RegistrationValidation.ThrowIfInvalidCompany(request);
        var intent = await _db.RegistrationIntents.SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new KeyNotFoundException("Registro inexistente.");
        if (intent.Status != "EmailVerified" || intent.CheckoutTokenHash is null ||
            !CryptographicOperations.FixedTimeEquals(
                Convert.FromHexString(intent.CheckoutTokenHash),
                Convert.FromHexString(Hash(checkoutToken))))
            throw new UnauthorizedAccessException("Registro no verificado.");
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
            ?? throw new KeyNotFoundException("Registro inexistente.");
        if (intent.Status != "EmailVerified" || intent.CheckoutTokenHash is null ||
            !CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(intent.CheckoutTokenHash),
                Encoding.UTF8.GetBytes(Hash(checkoutToken))))
            throw new UnauthorizedAccessException("Registro no verificado.");
        if (intent.HolderType is not ("Individual" or "LegalEntity") || string.IsNullOrWhiteSpace(intent.LegalName) || string.IsNullOrWhiteSpace(intent.Nif) ||
            string.IsNullOrWhiteSpace(intent.Address) || string.IsNullOrWhiteSpace(intent.PostalCode) ||
            string.IsNullOrWhiteSpace(intent.City) || string.IsNullOrWhiteSpace(intent.Province))
            throw new InvalidOperationException("Completa los datos fiscales antes de contratar.");
        if (intent.CheckoutSessionId is not null)
            throw new InvalidOperationException("Ya existe una sesión de pago. Consulta su estado antes de crear otra.");
        var plan = await _db.SubscriptionPlans.SingleOrDefaultAsync(x => x.Code == planCode && x.IsActive, ct)
            ?? throw new KeyNotFoundException("Plan no disponible.");
        var user = await _users.FindByIdAsync(intent.UserId) ?? throw new KeyNotFoundException("Usuario inexistente.");
        var (sessionId, url) = await _gateway.CreateSubscriptionCheckoutAsync(plan.StripePriceId, user.Email!,
            id.ToString("D"), _stripe.SuccessUrl, _stripe.CancelUrl, ct);
        intent.PlanCode = plan.Code;
        intent.CheckoutSessionId = sessionId;
        intent.Status = "CheckoutCreated";
        await _db.SaveChangesAsync(ct);
        return new CheckoutResponse(url);
    }
    private static string Hash(string s) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(s)));
}
