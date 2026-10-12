using Facturacion.Application.Interfaces;
using Facturacion.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Facturacion.Infrastructure.Persistence.Repositories;

public sealed class InvoicePaymentRepository : IInvoicePaymentRepository
{
    private readonly BillingDbContext _db;
    public InvoicePaymentRepository(BillingDbContext db) => _db = db;

    public Task<InvoicePayment?> GetByIdAsync(Guid id, CancellationToken ct) =>
        _db.InvoicePayments.FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task AddAsync(InvoicePayment entity, CancellationToken ct) =>
        await _db.InvoicePayments.AddAsync(entity, ct);

}
