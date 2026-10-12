using System.Security.Cryptography;
using System.Text;
using Facturacion.Application.Billing;
using Facturacion.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Facturacion.Payments.Stripe;

namespace Facturacion.Infrastructure.Billing;
public sealed class SaasRegistrationService
{
    private readonly SelfServiceDbContext _db;
    private readonly UserManager<ApplicationUser> _users;
    private readonly IRegistrationEmailSender _emails;
    private readonly IPaymentGateway _gateway;
    private readonly StripeOptions _stripe;
    public SaasRegistrationService(SelfServiceDbContext db, UserManager<ApplicationUser> users,
        IRegistrationEmailSender emails, IPaymentGateway gateway, IOptions<StripeOptions> stripe)
    { _db=db; _users=users; _emails=emails; _gateway=gateway; _stripe=stripe.Value; }

    public async Task<RegistrationResponse> RegisterAsync(RegisterSaasRequest r, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(r.Email) || string.IsNullOrWhiteSpace(r.TenantName) ||
            string.IsNullOrWhiteSpace(r.Nif) || string.IsNullOrWhiteSpace(r.LegalName) ||
            string.IsNullOrWhiteSpace(r.Address) || string.IsNullOrWhiteSpace(r.PostalCode) ||
            string.IsNullOrWhiteSpace(r.City) || string.IsNullOrWhiteSpace(r.Province))
            throw new ArgumentException("Faltan datos obligatorios de registro.");
        var email = r.Email.Trim();
        if (await _users.FindByEmailAsync(email) is not null)
            throw new InvalidOperationException("No se puede completar el registro con estos datos.");
        var user = new ApplicationUser { Email=email, UserName=email, DisplayName=r.DisplayName, IsActive=true, EmailConfirmed=false };
        var result = await _users.CreateAsync(user, r.Password);
        if (!result.Succeeded) throw new ArgumentException(string.Join("; ", result.Errors.Select(e=>e.Description)));
        var intent = new RegistrationIntent { UserId=user.Id, TenantName=r.TenantName.Trim(),
            Nif=r.Nif.Trim(), LegalName=r.LegalName.Trim(), Address=r.Address.Trim(),
            PostalCode=r.PostalCode.Trim(), City=r.City.Trim(), Province=r.Province.Trim() };
        _db.RegistrationIntents.Add(intent);
        await _db.SaveChangesAsync(ct);
        var token = await _users.GenerateEmailConfirmationTokenAsync(user);
        await _emails.SendConfirmationAsync(email, intent.Id, token, ct);
        return new RegistrationResponse(intent.Id, "Comprueba tu correo para verificar la cuenta.");
    }

    public async Task<string> VerifyEmailAsync(Guid id, string token, CancellationToken ct)
    {
        var intent = await _db.RegistrationIntents.SingleOrDefaultAsync(x=>x.Id==id,ct)
            ?? throw new KeyNotFoundException("Registro inexistente.");
        var user = await _users.FindByIdAsync(intent.UserId) ?? throw new KeyNotFoundException("Usuario inexistente.");
        var result = await _users.ConfirmEmailAsync(user,token);
        // Un token invalido NUNCA puede reemitir un token de checkout, ni siquiera
        // si el correo ya estaba confirmado: en ese caso la condicion combined
        // dejaba pasar cualquier cadena vacia de token.
        if (!result.Succeeded)
            throw new UnauthorizedAccessException("Token inválido o caducado.");
        var secret = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        intent.CheckoutTokenHash = Hash(secret);
        intent.Status = "EmailVerified";
        await _db.SaveChangesAsync(ct);
        return secret; // Solo entregar tras verificar el token del correo; nunca registrar en logs.
    }

    public async Task<CheckoutResponse> CheckoutAsync(Guid id, string checkoutToken, string planCode, CancellationToken ct)
    {
        var intent = await _db.RegistrationIntents.SingleOrDefaultAsync(x=>x.Id==id,ct)
            ?? throw new KeyNotFoundException("Registro inexistente.");
        if (intent.Status != "EmailVerified" || intent.CheckoutTokenHash is null ||
            !CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(intent.CheckoutTokenHash),
                Encoding.UTF8.GetBytes(Hash(checkoutToken))))
            throw new UnauthorizedAccessException("Registro no verificado.");
        if (intent.CheckoutSessionId is not null)
            throw new InvalidOperationException("Ya existe una sesión de pago. Consulta su estado antes de crear otra.");
        var plan = await _db.SubscriptionPlans.SingleOrDefaultAsync(x=>x.Code==planCode && x.IsActive,ct)
            ?? throw new KeyNotFoundException("Plan no disponible.");
        var user = await _users.FindByIdAsync(intent.UserId) ?? throw new KeyNotFoundException("Usuario inexistente.");
        var (sessionId,url) = await _gateway.CreateSubscriptionCheckoutAsync(plan.StripePriceId,user.Email!,
            id.ToString("D"),_stripe.SuccessUrl,_stripe.CancelUrl,ct);
        intent.PlanCode=plan.Code;
        intent.CheckoutSessionId=sessionId;
        intent.Status="CheckoutCreated";
        await _db.SaveChangesAsync(ct);
        return new CheckoutResponse(url);
    }
    private static string Hash(string s) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(s)));
}
