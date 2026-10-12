using Facturacion.Application.Interfaces;
using Facturacion.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Facturacion.Infrastructure.Persistence.Repositories;

public sealed class ElectronicDocumentRepository : IElectronicDocumentRepository
{
    private readonly BillingDbContext _db;
    public ElectronicDocumentRepository(BillingDbContext db) => _db = db;

    public Task<ElectronicDocument?> GetByIdAsync(Guid id, CancellationToken ct) =>
        _db.ElectronicDocuments.FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task AddAsync(ElectronicDocument entity, CancellationToken ct) =>
        await _db.ElectronicDocuments.AddAsync(entity, ct);

    public Task<ElectronicDocument?> GetByInvoiceAsync(
       Guid invoiceId,
       CancellationToken ct)
    {
        return _db.ElectronicDocuments
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.InvoiceId == invoiceId,
                ct);
    }

}
