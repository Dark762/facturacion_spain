using Facturacion.Domain.Entities;
namespace Facturacion.Application.Interfaces;
public interface IOutboxMessageRepository
{
    Task<IReadOnlyList<OutboxMessage>> GetPendingAsync(int maxCount, CancellationToken ct);
    Task<OutboxMessage?> GetByIdAsync(Guid id, CancellationToken ct);
    Task AddAsync(OutboxMessage entity, CancellationToken ct);
}
