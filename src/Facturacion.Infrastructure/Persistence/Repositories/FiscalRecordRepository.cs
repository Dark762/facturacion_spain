using Facturacion.Application.Interfaces;
using Facturacion.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Facturacion.Infrastructure.Persistence.Repositories;

public sealed class FiscalRecordRepository : IFiscalRecordRepository
{
    private readonly BillingDbContext _db;
    public FiscalRecordRepository(BillingDbContext db) => _db = db;

    public Task<FiscalRecord?> GetByIdAsync(Guid id, CancellationToken ct) =>
        _db.FiscalRecords.FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task AddAsync(FiscalRecord entity, CancellationToken ct) =>
        await _db.FiscalRecords.AddAsync(entity, ct);

    public async Task<IReadOnlyList<FiscalRecord>> GetByInvoiceAsync(Guid invoiceId, CancellationToken ct) =>
        await _db.FiscalRecords.AsNoTracking()
            .Where(x => x.InvoiceId == invoiceId)
            .OrderBy(x => x.GeneratedAt).ToListAsync(ct);

}
