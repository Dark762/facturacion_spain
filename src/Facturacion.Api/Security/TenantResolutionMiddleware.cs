using System.Security.Claims;
using Facturacion.Infrastructure.Identity;
using Facturacion.Infrastructure.Multitenancy;
using Microsoft.EntityFrameworkCore;
namespace Facturacion.Api.Security;
public sealed class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;
    public TenantResolutionMiddleware(RequestDelegate next) => _next = next;
    public async Task InvokeAsync(HttpContext http, AuthDbContext auth, TenantContext tenant)
    {
        if (http.User.Identity?.IsAuthenticated == true)
        {
            var userId = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
            var raw = http.User.FindFirst("tenant_id")?.Value;
            if (string.IsNullOrWhiteSpace(userId) || !Guid.TryParse(raw, out var tenantId))
            { http.Response.StatusCode = StatusCodes.Status401Unauthorized; return; }
            var valid = await auth.TenantUsers.AsNoTracking().AnyAsync(
                x => x.UserId == userId && x.TenantId == tenantId && x.IsActive, http.RequestAborted);
            if (!valid) { http.Response.StatusCode = StatusCodes.Status403Forbidden; return; }
            tenant.SetTenant(tenantId);
        }
        await _next(http);
    }
}
