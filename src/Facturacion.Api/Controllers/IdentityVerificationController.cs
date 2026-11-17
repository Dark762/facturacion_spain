using System.Security.Claims;
using Facturacion.Api.Responses;
using Facturacion.Api.Security;
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
public sealed class IdentityVerificationController(SelfServiceDbContext db, CertificateValidationService certificates) : ControllerBase
{
    public sealed record StartRequest(string Method, string SubjectKind);

    /// <summary>Capacidades reales: ninguna vía se anuncia como verificada sin un proveedor confiable.</summary>
    [HttpGet("capabilities")]
    public IActionResult Capabilities() => this.ApiOk(new
    {
        Certificate = new { Available = false, Reason = "Pendiente de configurar autenticación mTLS, validación de cadena, OCSP/CRL y prueba de representación." },
        Manual = new { Available = false, Reason = "Pendiente de contratar un proveedor documental, verificar sus webhooks y proteger las evidencias." },
        Individual = new { Supported = false },
        LegalEntity = new { Supported = false, RequiresRepresentation = true },
        Sandbox = new { Available = false, Reason = "No se habilitan aprobaciones simuladas en el sistema real." }
    });

    /// <summary>Impide presentar una solicitud como sesión de identificación real.</summary>
    [HttpPost("{id:guid}/begin-proof")]
    public async Task<IActionResult> BeginProof(Guid id, CancellationToken ct)
    {
        if (!TryScope(out var userId, out var tenantId)) return Forbid();
        var exists = await db.IdentityVerifications.AsNoTracking().AnyAsync(
            x => x.Id == id && x.UserId == userId && x.TenantId == tenantId &&
                (x.Status == "AwaitingEvidence" || x.Status == "AwaitingCertificate"), ct);
        if (!exists) return this.ApiFailure<object>(404, "VERIFICATION_NOT_FOUND", "Solicitud no encontrada o no pendiente.");
        return this.ApiFailure<object>(503, "IDENTITY_PROVIDER_NOT_CONFIGURED",
            "No existe un proveedor de identificación conectado. No se puede validar identidad ni representación.");
    }

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
            CanIssueWithVerifiedIdentity = false, // Sin evidencia validada ni política de autorización habilitada.
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


    /// <summary>
    /// Comprueba la prueba criptográfica TLS; NO aprueba identidad fiscal ni representación.
    /// El certificado solo puede proceder del handshake TLS de la conexión con la API.
    /// Si se usa proxy TLS, este endpoint no debe exponerse hasta implementar una
    /// autenticación de proxy de confianza equivalente.
    /// </summary>
    [HttpPost("{id:guid}/certificate-check")]
    public async Task<IActionResult> CheckCertificate(Guid id, CancellationToken ct)
    {
        if (!TryScope(out var userId, out var tenantId)) return Forbid();
        var pending = await db.IdentityVerifications.AsNoTracking().AnyAsync(
            x => x.Id == id && x.UserId == userId && x.TenantId == tenantId &&
                x.Method == "Certificate" && x.Status == "AwaitingCertificate", ct);
        if (!pending)
            return this.ApiFailure<object>(404, "VERIFICATION_NOT_FOUND",
                "No existe una solicitud de certificado pendiente para este usuario.");
        if (!HttpContext.Request.IsHttps)
            return this.ApiFailure<object>(403, "TLS_REQUIRED", "Se requiere conexión TLS directa.");

        // No confiar en cabeceras X-Client-Cert ni en certificados recibidos por JSON.
        var clientCertificate = await HttpContext.Connection.GetClientCertificateAsync(ct);
        if (clientCertificate is null)
            return this.ApiFailure<object>(401, "CLIENT_CERTIFICATE_REQUIRED",
                "No se presentó un certificado cliente durante la negociación TLS.");
        using (clientCertificate)
        {
            var result = certificates.Validate(clientCertificate);
            if (!result.Valid)
                return this.ApiFailure<object>(401, result.Code,
                    "El certificado no ha superado la validación criptográfica.");
            // Nunca devolver NIF ni atributos personales en este paso.
            // Falta comparar titular fiscal y representación antes de Verified.
            return this.ApiOk(new
            {
                CertificateValidated = true,
                FiscalIdentityVerified = false,
                RepresentationVerified = false,
                VerificationStatus = "AwaitingCertificate",
                CertificateExpiresAt = result.Attributes!.ExpiresAt
            });
        }
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
