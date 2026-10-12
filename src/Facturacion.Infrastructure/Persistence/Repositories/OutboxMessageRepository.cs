using Facturacion.Application.Interfaces;
using Facturacion.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Facturacion.Infrastructure.Persistence.Repositories;

public sealed class OutboxMessageRepository : IOutboxMessageRepository
{
    private readonly BillingDbContext _db;
    public OutboxMessageRepository(BillingDbContext db) => _db = db;

    public Task<OutboxMessage?> GetByIdAsync(Guid id, CancellationToken ct) =>
        _db.OutboxMessages.FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task AddAsync(OutboxMessage entity, CancellationToken ct) =>
        await _db.OutboxMessages.AddAsync(entity, ct);

    // Solo el tenant actual. Un worker debe establecer su contexto antes de llamar.
    // No reclama mensajes en exclusiva: para varios workers implementar claim atómico.
    public async Task<IReadOnlyList<OutboxMessage>> GetPendingAsync(int maxCount, CancellationToken ct)
    {
        // Se evalua fuera del arbol de consultas: no todos los proveedores
        // traducen DateTimeOffset.UtcNow a SQL.
        var now = DateTimeOffset.UtcNow;

        return await _db.OutboxMessages.AsNoTracking()
            .Where(x => x.Status == "Pending" && (x.NextAttemptAt == null || x.NextAttemptAt <= now))
            .OrderBy(x => x.CreatedAt)
            .Take(Math.Clamp(maxCount, 1, 500)).ToListAsync(ct);
    }

}
