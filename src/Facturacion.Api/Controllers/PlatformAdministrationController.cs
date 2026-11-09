using Facturacion.Api.Responses;
using Facturacion.Infrastructure.Billing;
using Facturacion.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Facturacion.Api.Controllers;

/// <summary>Lectura global exclusiva de la identidad propietaria de la plataforma.</summary>
[ApiController]
[Authorize(Policy = "PlatformAdmin")]
[Route("api/platform")]
public sealed class PlatformAdministrationController(
    BillingDbContext billing, SelfServiceDbContext selfService) : ControllerBase
{
    public sealed record TenantOverview(
        Guid TenantId, string Name, bool IsActive, int ActiveCompanies,
        string? PlanCode, string? SubscriptionStatus, DateTimeOffset? RegisteredAt);

    public sealed record PlatformSummary(int TotalTenants, int ActiveTenants, int ActiveCompanies);

    [HttpGet("summary")]
    public async Task<IActionResult> Summary(CancellationToken ct)
    {
        // Consulta global explícita; jamás exponer este contexto fuera de la política PlatformAdmin.
        var total = await billing.Tenants.AsNoTracking().CountAsync(ct);
        var active = await billing.Tenants.AsNoTracking().CountAsync(x => x.IsActive, ct);
        var companies = await billing.Companys.IgnoreQueryFilters().AsNoTracking()
            .CountAsync(x => x.IsActive, ct);
        return this.ApiOk(new PlatformSummary(total, active, companies));
    }

    [HttpGet("tenants")]
    public async Task<IActionResult> Tenants([FromQuery] int page = 1,
        [FromQuery] int pageSize = 25, CancellationToken ct = default)
    {
        if (page < 1 || pageSize is < 1 or > 100)
            return this.ApiFailure<object>(400, "INVALID_PAGINATION",
                "La página debe ser positiva y el tamaño debe estar entre 1 y 100.");

        var tenants = await billing.Tenants.AsNoTracking()
            .OrderBy(x => x.Name).ThenBy(x => x.Id)
            .Skip((int)Math.Min((long)(page - 1) * pageSize, int.MaxValue))
            .Take(pageSize)
            .Select(x => new { x.Id, x.Name, x.IsActive })
            .ToListAsync(ct);
        var ids = tenants.Select(x => x.Id).ToArray();
        var counts = await billing.Companys.IgnoreQueryFilters().AsNoTracking()
            .Where(x => ids.Contains(x.TenantId) && x.IsActive)
            .GroupBy(x => x.TenantId)
            .Select(x => new { TenantId = x.Key, Count = x.Count() })
            .ToDictionaryAsync(x => x.TenantId, x => x.Count, ct);
        var subscriptions = await selfService.SubscriptionStates.AsNoTracking()
            .Where(x => ids.Contains(x.TenantId))
            .Select(x => new { x.TenantId, x.PlanCode, x.Status })
            .ToDictionaryAsync(x => x.TenantId, ct);
        var registrations = await selfService.RegistrationIntents.AsNoTracking()
            .Where(x => x.TenantId.HasValue && ids.Contains(x.TenantId.Value))
            .GroupBy(x => x.TenantId!.Value)
            .Select(x => new { TenantId = x.Key, CreatedAt = x.Min(y => y.CreatedAt) })
            .ToDictionaryAsync(x => x.TenantId, x => x.CreatedAt, ct);
        var result = tenants.Select(x =>
        {
            subscriptions.TryGetValue(x.Id, out var subscription);
            var hasRegistration = registrations.TryGetValue(x.Id, out var registration);
            return new TenantOverview(x.Id, x.Name, x.IsActive,
                counts.GetValueOrDefault(x.Id), subscription?.PlanCode,
                subscription?.Status, hasRegistration ? registration : (DateTimeOffset?)null);
        }).ToArray();
        return this.ApiOk(result);
    }
}
