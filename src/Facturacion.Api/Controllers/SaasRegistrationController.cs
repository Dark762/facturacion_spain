using Facturacion.Application.Billing;
using Facturacion.Infrastructure.Billing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace Facturacion.Api.Controllers;
[ApiController, Route("api/saas"), AllowAnonymous]
public sealed class SaasRegistrationController : ControllerBase
{
    private readonly SaasRegistrationService _registration;
    private readonly SelfServiceDbContext _db;
    public SaasRegistrationController(SaasRegistrationService registration, SelfServiceDbContext db)
    { _registration=registration; _db=db; }
    [HttpGet("plans")]
    public async Task<IActionResult> Plans(CancellationToken ct) =>
        Ok(await _db.SubscriptionPlans.AsNoTracking().Where(x=>x.IsActive)
            .Select(x=>new PlanResponse(x.Code,x.Name)).ToListAsync(ct));
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterSaasRequest request,CancellationToken ct) =>
        Ok(await _registration.RegisterAsync(request,ct));
    public sealed record VerifyRequest(Guid RegistrationId,string Token);
    [HttpPost("verify-email")]
    public async Task<IActionResult> Verify([FromBody] VerifyRequest r,CancellationToken ct) =>
        Ok(new { CheckoutToken=await _registration.VerifyEmailAsync(r.RegistrationId,r.Token,ct) });
    public sealed record CheckoutRequest(Guid RegistrationId,string CheckoutToken,string PlanCode);
    [HttpPost("checkout")]
    public async Task<IActionResult> Checkout([FromBody] CheckoutRequest r,CancellationToken ct) =>
        Ok(await _registration.CheckoutAsync(r.RegistrationId,r.CheckoutToken,r.PlanCode,ct));
}
