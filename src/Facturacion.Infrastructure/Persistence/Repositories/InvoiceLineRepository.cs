using Facturacion.Application.Interfaces;
using Facturacion.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Facturacion.Infrastructure.Persistence.Repositories;

public sealed class InvoiceLineRepository : IInvoiceLineRepository
{
    private readonly BillingDbContext _db;
    public InvoiceLineRepository(BillingDbContext db) => _db = db;

    public Task<InvoiceLine?> GetByIdAsync(Guid id, CancellationToken ct) =>
        _db.InvoiceLines.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task AddAsync(InvoiceLine entity, CancellationToken ct) =>
        await _db.InvoiceLines.AddAsync(entity, ct);

}
