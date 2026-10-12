using Facturacion.Application.Interfaces;
using Facturacion.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Facturacion.Infrastructure.Persistence.Repositories;

public sealed class AuditEventRepository : IAuditEventRepository
{
    private readonly BillingDbContext _db;
    public AuditEventRepository(BillingDbContext db) => _db = db;

    public Task<AuditEvent?> GetByIdAsync(Guid id, CancellationToken ct) =>
        _db.AuditEvents.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task AddAsync(AuditEvent entity, CancellationToken ct) =>
        await _db.AuditEvents.AddAsync(entity, ct);

}
