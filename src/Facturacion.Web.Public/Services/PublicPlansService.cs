using Facturacion.Application.Billing;
using Facturacion.Infrastructure.Billing;
using Microsoft.EntityFrameworkCore;

namespace Facturacion.Web.Public.Services;

/// <summary>Consulta de planes públicos. No expone el DbContext a componentes Razor.</summary>
public sealed class PublicPlansService(SelfServiceDbContext db)
{
    public Task<List<PlanResponse>> GetActiveAsync(CancellationToken cancellationToken = default) =>
        db.SubscriptionPlans.AsNoTracking()
            .Where(plan => plan.IsActive)
            .OrderBy(plan => plan.Name)
            .Select(plan => new PlanResponse(plan.Code, plan.Name))
            .ToListAsync(cancellationToken);
}
