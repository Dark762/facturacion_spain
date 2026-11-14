using System.Security.Claims;
using Facturacion.Api.Responses;
using Facturacion.Configuration.Security;
using Facturacion.Infrastructure.Billing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Facturacion.Api.Controllers;

/// <summary>Solicitudes de identificación. No acepta certificados ni DNI enviados por el navegador como prueba de identidad.</summary>
[ApiController]
[Authorize]
[Route("api/identity-verification")]
public sealed class IdentityVerificationController(SelfServiceDbContext db) : ControllerBase
{
    public sealed record StartRequest(string Method, string SubjectKind);

    /// <summary>Estado informativo, sin habilitar todavía controles de emisión.</summary>
    [HttpGet("readiness")]
    public async Task<IActionResult> Readiness(CancellationToken ct)
    {
        if (!TryScope(out var userId, out var tenantId)) return Forbid();
        var verified = await db.IdentityVerifications.AsNoTracking().AnyAsync(
            x => x.UserId == userId && x.TenantId == tenantId && x.Status == "Verified", ct);
        var pending = await db.IdentityVerifications.AsNoTracking().AnyAsync(
            x => x.UserId == userId && x.TenantId == tenantId &&
                (x.Status == "AwaitingEvidence" || x.Status == "AwaitingCertificate" ||
                 x.Status == "UnderReview"), ct);
        return this.ApiOk(new
        {
            IdentityVerified = verified,
            VerificationPending = pending,
            CanIssueWithVerifiedIdentity = verified,
            RepresentationVerified = false,
            CanActAsCompanyRepresentative = false,
            EnforcementEnabled = false
        });
    }

    [HttpGet("mine")]
    public async Task<IActionResult> Mine(CancellationToken ct)
    {
        if (!TryScope(out var userId, out var tenantId)) return Forbid();
        var requests = await db.IdentityVerifications.AsNoTracking()
            .Where(x => x.UserId == userId && x.TenantId == tenantId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new { x.Id, x.Method, x.SubjectKind, x.Status, x.CreatedAt, x.UpdatedAt, x.DecisionCode })
            .Take(20).ToArrayAsync(ct);
        return this.ApiOk(requests);
    }

    [HttpPost]
    public async Task<IActionResult> Start([FromBody] StartRequest request, CancellationToken ct)
    {
        if (!TryScope(out var userId, out var tenantId)) return Forbid();
        if (request.Method is not ("Manual" or "Certificate") ||
            request.SubjectKind is not ("Individual" or "LegalEntity"))
            return this.ApiFailure<object>(400, "INVALID_VERIFICATION_METHOD", "Método o tipo de titular no válido.");

        // No conceder estado Verified por introducir datos o seleccionar un método.
        var existing = await db.IdentityVerifications.AsNoTracking().AnyAsync(
            x => x.UserId == userId && x.TenantId == tenantId &&
                x.Status != "Rejected" && x.Status != "Cancelled", ct);
        if (existing)
            return this.ApiFailure<object>(409, "VERIFICATION_ALREADY_EXISTS", "Ya existe una solicitud vigente.");

        var item = new IdentityVerification
        {
            UserId = userId, TenantId = tenantId, Method = request.Method,
            SubjectKind = request.SubjectKind,
            Status = request.Method == "Manual" ? "AwaitingEvidence" : "AwaitingCertificate"
        };
        db.IdentityVerifications.Add(item);
        await db.SaveChangesAsync(ct);
        return this.ApiOk(new { item.Id, item.Method, item.SubjectKind, item.Status });
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
    {
        if (!TryScope(out var userId, out var tenantId)) return Forbid();
        var now = DateTimeOffset.UtcNow;
        // Transición atómica: impide cancelar una solicitud aprobada/rechazada
        // por otro proceso entre la lectura y la escritura.
        var affected = await db.IdentityVerifications
            .Where(x => x.Id == id && x.UserId == userId && x.TenantId == tenantId &&
                (x.Status == "AwaitingEvidence" || x.Status == "AwaitingCertificate" ||
                 x.Status == "UnderReview"))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Status, "Cancelled")
                .SetProperty(x => x.UpdatedAt, now), ct);
        if (affected == 0)
            return this.ApiFailure<object>(409, "VERIFICATION_NOT_CANCELLABLE",
                "La solicitud no existe o no admite cancelación.");
        return this.ApiOk(new { Id = id, Status = "Cancelled", UpdatedAt = now });
    }

    private bool TryScope(out string userId, out Guid tenantId)
    {
        userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
        tenantId = Guid.Empty;
        return userId.Length > 0 &&
            Guid.TryParse(User.FindFirstValue(FacturacionClaim.TenantId.ToClaimType()), out tenantId) &&
            User.FindFirstValue(FacturacionClaim.Purpose.ToClaimType()) != FacturacionClaimPurpose.Platform.ToClaimValue();
    }
}
