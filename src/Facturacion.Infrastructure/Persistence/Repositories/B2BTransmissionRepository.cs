using Facturacion.Application.Interfaces;
using Facturacion.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Facturacion.Infrastructure.Persistence.Repositories;

public sealed class B2BTransmissionRepository : IB2BTransmissionRepository
{
    private readonly BillingDbContext _db;
    public B2BTransmissionRepository(BillingDbContext db) => _db = db;

    public Task<B2BTransmission?> GetByIdAsync(Guid id, CancellationToken ct) =>
        _db.B2BTransmissions.FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task AddAsync(B2BTransmission entity, CancellationToken ct) =>
        await _db.B2BTransmissions.AddAsync(entity, ct);

}
