using System.Security.Claims;
using Facturacion.Api.Responses;
using Facturacion.Infrastructure.Identity;
using Facturacion.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Facturacion.Api.Controllers;

/// <summary>Empresas activas a las que el usuario autenticado pertenece dentro de su tenant.</summary>
[ApiController]
[Authorize]
[Route("api/my/companies")]
public sealed class MyCompaniesController : ControllerBase
{
    private readonly AuthDbContext _auth;
    private readonly BillingDbContext _billing;

    public MyCompaniesController(AuthDbContext auth, BillingDbContext billing)
    {
        _auth = auth;
        _billing = billing;
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var tenantClaim = User.FindFirstValue("tenant_id");
        if (string.IsNullOrWhiteSpace(userId) || !Guid.TryParse(tenantClaim, out var tenantId))
            return this.ApiFailure<object>(401, "UNAUTHORIZED", "Sesión no válida.");

        var tenantMember = await _auth.TenantUsers.AsNoTracking()
            .AnyAsync(x => x.TenantId == tenantId && x.UserId == userId && x.IsActive, ct);
        if (!tenantMember)
            return this.ApiFailure<object>(403, "FORBIDDEN", "No tienes acceso a esta organización.");

        var ids = await _auth.CompanyUsers.AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.UserId == userId && x.IsActive)
            .Select(x => x.CompanyId).Distinct().ToListAsync(ct);

        var companies = await _billing.Companys.AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.IsActive && ids.Contains(x.Id))
            .OrderBy(x => x.LegalName)
            .Select(x => new { x.Id, x.LegalName })
            .ToListAsync(ct);
        return this.ApiOk(companies);
    }
}
