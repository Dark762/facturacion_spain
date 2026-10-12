using Facturacion.Application.Interfaces;
namespace Facturacion.Infrastructure.Multitenancy;
public sealed class TenantContext : ITenantContext
{
    private Guid? _tenantId;
    public Guid TenantId => _tenantId ?? throw new InvalidOperationException("Tenant no establecido.");
    public void SetTenant(Guid id)
    {
        if (id == Guid.Empty || _tenantId.HasValue) throw new InvalidOperationException("Tenant inválido o ya establecido.");
        _tenantId = id;
    }
}
