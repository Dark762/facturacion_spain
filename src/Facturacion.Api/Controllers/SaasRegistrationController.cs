using Facturacion.Application.Billing;
using Facturacion.Infrastructure.Billing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace Facturacion.Api.Controllers;
/// <summary>
/// API pública del alta autoservicio: consulta de planes, registro,
/// verificación de correo y creación de una sesión de checkout.
/// </summary>
/// <remarks>
/// El checkout no confirma el pago ni activa la suscripción.
/// La activación y el aprovisionamiento se gestionan después del webhook de Stripe.
/// </remarks>
[ApiController, Route("api/saas"), AllowAnonymous]
public sealed class SaasRegistrationController : ControllerBase
{
    private readonly ISaasRegistrationService _registration;
    private readonly SelfServiceDbContext _db;
    public SaasRegistrationController(ISaasRegistrationService registration, SelfServiceDbContext db)
    { _registration=registration; _db=db; }
    /// <summary>Devuelve los códigos y nombres de los planes activos.</summary>
    [HttpGet("plans")]
    public async Task<IActionResult> Plans(CancellationToken ct) =>
    Ok(await _db.SubscriptionPlans
        .AsNoTracking()
        .Where(x => x.IsActive)
        .OrderBy(x => x.DisplayOrder)
        .ThenBy(x => x.Name)
        .Select(x => new PlanResponse(
            x.Code,
            x.Name,
            x.MonthlyPrice,
            x.Currency,
            x.MaxCompanies,
            x.MaxUsers,
            x.MaxInvoicesPerMonth,
            x.Description))
        .ToListAsync(ct));

    /// <summary>Inicia el registro; delega las validaciones y la persistencia en el servicio.</summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterSaasRequest request, CancellationToken ct)
    {
        try { return Ok(await _registration.RegisterAsync(request, ct)); }
        catch (RegistrationValidationException ex) { return ValidationProblem(new ValidationProblemDetails(ex.Errors.ToDictionary(x => x.Key, x => x.Value))); }
    }
    /// <summary>Datos de identificación del registro y confirmación del correo.</summary>
    public sealed record VerifyRequest(Guid RegistrationId,string Token);
    /// <summary>Verifica el correo y devuelve el token que autoriza continuar al checkout.</summary>
    [HttpPost("verify-email")]
    public async Task<IActionResult> Verify([FromBody] VerifyRequest r,CancellationToken ct) =>
        Ok(new { CheckoutToken=await _registration.VerifyEmailAsync(r.RegistrationId,r.Token,ct) });
    /// <summary>Registro, token de checkout y código del plan elegido.</summary>
    public sealed record CheckoutRequest(Guid RegistrationId,string CheckoutToken,string PlanCode);
    /// <summary>Crea una sesión de Stripe Checkout, sin activar todavía la cuenta.</summary>
    [HttpPost("checkout")]
    public async Task<IActionResult> Checkout([FromBody] CheckoutRequest r,CancellationToken ct) =>
        Ok(await _registration.CheckoutAsync(r.RegistrationId,r.CheckoutToken,r.PlanCode,ct));
}
