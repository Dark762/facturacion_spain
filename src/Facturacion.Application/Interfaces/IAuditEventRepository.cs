using Facturacion.Domain.Entities;
namespace Facturacion.Application.Interfaces;
public interface IAuditEventRepository
{
    Task<AuditEvent?> GetByIdAsync(Guid id, CancellationToken ct);
    Task AddAsync(AuditEvent entity, CancellationToken ct);
}
