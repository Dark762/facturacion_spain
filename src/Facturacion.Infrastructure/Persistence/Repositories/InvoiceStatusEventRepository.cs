using Facturacion.Application.Interfaces;
using Facturacion.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Facturacion.Infrastructure.Persistence.Repositories;

public sealed class InvoiceStatusEventRepository : IInvoiceStatusEventRepository
{
    private readonly BillingDbContext _db;
    public InvoiceStatusEventRepository(BillingDbContext db) => _db = db;

    public Task<InvoiceStatusEvent?> GetByIdAsync(Guid id, CancellationToken ct) =>
        _db.InvoiceStatusEvents.FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task AddAsync(InvoiceStatusEvent entity, CancellationToken ct) =>
        await _db.InvoiceStatusEvents.AddAsync(entity, ct);

}
