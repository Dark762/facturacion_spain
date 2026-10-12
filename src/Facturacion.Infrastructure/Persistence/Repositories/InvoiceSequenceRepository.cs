using Facturacion.Application.Interfaces;
using Facturacion.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Facturacion.Infrastructure.Persistence.Repositories;

public sealed class InvoiceSequenceRepository : IInvoiceSequenceRepository
{
    private readonly BillingDbContext _db;
    public InvoiceSequenceRepository(BillingDbContext db) => _db = db;

    public Task<InvoiceSequence?> GetByIdAsync(Guid id, CancellationToken ct) =>
        _db.InvoiceSequences.FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task AddAsync(InvoiceSequence entity, CancellationToken ct) =>
        await _db.InvoiceSequences.AddAsync(entity, ct);

    // Tracking intencional: la secuencia se actualiza dentro de una transacción.
    // Este método NO aplica bloqueo FOR UPDATE; no usarlo solo para reservar números.
    public Task<InvoiceSequence?> GetBySeriesAsync(Guid companyId, string series, int fiscalYear, CancellationToken ct) =>
        _db.InvoiceSequences.FirstOrDefaultAsync(
            x => x.CompanyId == companyId && x.Series == series && x.FiscalYear == fiscalYear, ct);

}
