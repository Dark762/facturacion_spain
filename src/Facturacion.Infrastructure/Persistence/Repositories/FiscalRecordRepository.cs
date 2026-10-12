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
        // La ordenacion se hace en memoria a proposito: SQLite no admite
        // ORDER BY sobre DateTimeOffset, y las pruebas corren sobre el. El
        // numero de registros por factura es pequeno, asi que el coste es
        // irrelevante y el repositorio funciona en los cuatro motores.
        (await _db.FiscalRecords.AsNoTracking()
            .Where(x => x.InvoiceId == invoiceId)
            .ToListAsync(ct))
            .OrderBy(x => x.GeneratedAt)
            .ToList();

}
