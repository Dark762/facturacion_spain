using System.Security.Claims;
using Facturacion.Application.Provisioning;
using Facturacion.Infrastructure.Identity;
using Facturacion.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Facturacion.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/tenant-admin")]
public sealed class TenantAdministrationController : ControllerBase
{
    private readonly IProvisioningService _provisioning;
    private readonly AuthDbContext _auth;
    private readonly BillingDbContext _billing;

    public TenantAdministrationController(IProvisioningService provisioning, AuthDbContext auth, BillingDbContext billing)
    { _provisioning = provisioning; _auth = auth; _billing = billing; }

    // El JWT identifica el tenant; NUNCA aceptar TenantId desde el body.
    private async Task<Guid?> GetAdminTenantAsync(CancellationToken ct)
    {
        var id = User.FindFirstValue("tenant_id");
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(id, out var tenantId) || string.IsNullOrWhiteSpace(userId)) return null;
        var hasActiveMembership = await _auth.TenantUsers.AnyAsync(x => x.TenantId == tenantId && x.UserId == userId && x.IsActive, ct);
        if (!hasActiveMembership) return null;
        // Política de administración del tenant: al menos una empresa activa con rol Admin.
        var isAdmin = await _auth.CompanyUsers.AnyAsync(x => x.TenantId == tenantId && x.UserId == userId && x.IsActive && x.Role == "Admin", ct);
        return isAdmin ? tenantId : null;
    }

    [HttpPost("companies")]
    public async Task<IActionResult> CreateCompany([FromBody] CreateCompanyRequest request, CancellationToken ct)
    {
        var tenantId = await GetAdminTenantAsync(ct);
        if (tenantId is null) return Forbid();
        var result = await _provisioning.CreateCompanyAsync(tenantId.Value, request, ct);
        return Ok(result);
    }

/// <summary>
    /// retired: permitir a un administrador fijar la contraseña de otro usuario es una
    /// escala de privilegios. El alta de usuarios se hace exclusivamente mediante
    /// invitaciones (/api/invitations), que obligan al invitado a establecer su
    /// propia credencial.
    /// </summary>
    [HttpPost("users")]
    [Obsolete("Sustituir por POST /api/invitations. Se conserva solo para no romper clientes existentes.")]
    public IActionResult CreateUser() =>
        StatusCode(StatusCodes.Status410Gone,
            new { message = "Este endpoint ha sido retirado. Usa POST /api/invitations." });

    [HttpPut("companies/{companyId:guid}/users")]
    public async Task<IActionResult> AssignCompanyUser(Guid companyId, [FromBody] AssignCompanyUserRequest request, CancellationToken ct)
    {
        var tenantId = await GetAdminTenantAsync(ct);
        if (tenantId is null) return Forbid();
        await _provisioning.AssignCompanyUserAsync(tenantId.Value, companyId, request, ct);
        return NoContent();
    }
}
