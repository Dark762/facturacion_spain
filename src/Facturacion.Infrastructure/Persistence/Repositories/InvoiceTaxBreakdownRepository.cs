using Facturacion.Application.Interfaces;
using Facturacion.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Facturacion.Infrastructure.Persistence.Repositories;

public sealed class InvoiceTaxBreakdownRepository : IInvoiceTaxBreakdownRepository
{
    private readonly BillingDbContext _db;
    public InvoiceTaxBreakdownRepository(BillingDbContext db) => _db = db;

    public Task<InvoiceTaxBreakdown?> GetByIdAsync(Guid id, CancellationToken ct) =>
        _db.InvoiceTaxBreakdowns.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task AddAsync(InvoiceTaxBreakdown entity, CancellationToken ct) =>
        await _db.InvoiceTaxBreakdowns.AddAsync(entity, ct);

}
