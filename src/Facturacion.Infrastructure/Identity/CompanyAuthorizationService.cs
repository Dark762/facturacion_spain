using Facturacion.Application.Common.Results;
using Facturacion.Application.Interfaces;
using Facturacion.Infrastructure.Multitenancy;
using Facturacion.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
namespace Facturacion.Infrastructure.Identity;
public sealed class CompanyAuthorizationService : ICompanyAuthorizationService
{
    private readonly AuthDbContext _auth;
    private readonly BillingDbContext _billing;
    private readonly ICurrentUserService _user;
    private readonly ITenantContext _tenant;
    public CompanyAuthorizationService(AuthDbContext auth, BillingDbContext billing, ICurrentUserService user, ITenantContext tenant)
    { _auth=auth; _billing=billing; _user=user; _tenant=tenant; }
    public async Task<Result> AuthorizeAsync(Guid companyId, string permission, CancellationToken ct)
    {
        if (!_user.IsAuthenticated || string.IsNullOrWhiteSpace(_user.UserId))
            return Result.Failure(new Error("UNAUTHORIZED", "Sesión no válida.", ErrorType.Unauthorized));
        var tid = _tenant.TenantId;
        var member = await _auth.TenantUsers.AsNoTracking().AnyAsync(x => x.TenantId == tid && x.UserId == _user.UserId && x.IsActive, ct);
        if (!member) return Denied();
        var company = await _billing.Companys.AsNoTracking().AnyAsync(x => x.Id == companyId && x.TenantId == tid && x.IsActive, ct);
        if (!company) return Denied();
        var role = await _auth.CompanyUsers.AsNoTracking()
            .Where(x => x.TenantId == tid && x.CompanyId == companyId && x.UserId == _user.UserId && x.IsActive)
            .Select(x => x.Role).FirstOrDefaultAsync(ct);
        bool allowed = permission switch
        {
            "invoices.read" => role is "Reader" or "Editor" or "Admin",
            "invoices.create" => role is "Editor" or "Admin",
            "invoices.issue" => role is "Admin",
            _ => false
        };
        return allowed ? Result.Success() : Denied();
    }
    private static Result Denied() => Result.Failure(new Error("ACCESS_DENIED", "No tiene permiso sobre esta empresa.", ErrorType.Forbidden));
}
