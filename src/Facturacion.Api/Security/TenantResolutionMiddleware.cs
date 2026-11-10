using System.Security.Claims;
using Facturacion.Configuration.Security;
using Facturacion.Infrastructure.Identity;
using Facturacion.Observability;
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
            var purpose = http.User.FindFirst(FacturacionClaim.Purpose.ToClaimType())?.Value;
            var path = http.Request.Path;
            // La preautenticación solo puede invocar la elección de tenant.
            if (purpose == FacturacionClaimPurpose.TenantSelection.ToClaimValue())
            {
                if (path.Equals("/api/auth/login/complete", StringComparison.OrdinalIgnoreCase))
                { await _next(http); return; }
                http.Response.StatusCode = StatusCodes.Status403Forbidden;
                return;
            }
            // El token de plataforma no habilita rutas ordinarias ni consultas de negocio.
            if (purpose == FacturacionClaimPurpose.Platform.ToClaimValue())
            {
                if (path.StartsWithSegments("/api/platform"))
                { await _next(http); return; }
                http.Response.StatusCode = StatusCodes.Status403Forbidden;
                return;
            }
            var userId = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
            var raw = http.User.FindFirst(FacturacionClaim.TenantId.ToClaimType())?.Value;
            if (string.IsNullOrWhiteSpace(userId) || !Guid.TryParse(raw, out var tenantId))
            { http.Response.StatusCode = StatusCodes.Status401Unauthorized; return; }
            var valid = await auth.TenantUsers.AsNoTracking().AnyAsync(
                x => x.UserId == userId && x.TenantId == tenantId && x.IsActive, http.RequestAborted);
            if (!valid) { http.Response.StatusCode = StatusCodes.Status403Forbidden; return; }
            tenant.SetTenant(tenantId);
            // Solo después de verificar la pertenencia activa al tenant.
            http.Items[HttpRequestLogEnricher.ValidatedTenantIdKey] = tenantId.ToString("D");
            http.Items[HttpRequestLogEnricher.ValidatedUserIdKey] = userId;
        }
        await _next(http);
    }
}
