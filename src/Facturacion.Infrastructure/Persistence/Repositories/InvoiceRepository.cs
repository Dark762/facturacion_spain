using Facturacion.Application.Interfaces;
using Facturacion.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Facturacion.Infrastructure.Persistence.Repositories;

public sealed class InvoiceRepository : IInvoiceRepository
{
    private readonly BillingDbContext _db;
    public InvoiceRepository(BillingDbContext db) => _db = db;

    public Task<Invoice?> GetByIdAsync(Guid id, CancellationToken ct) =>
        _db.Invoices.FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task AddAsync(Invoice entity, CancellationToken ct) =>
        await _db.Invoices.AddAsync(entity, ct);

    public Task<Company?> GetCompanyAsync(Guid companyId, CancellationToken ct) =>
        _db.Companys.AsNoTracking().FirstOrDefaultAsync(x => x.Id == companyId, ct);

    public Task<Customer?> GetCustomerAsync(Guid companyId, Guid customerId, CancellationToken ct) =>
        _db.Customers.AsNoTracking().FirstOrDefaultAsync(
            x => x.Id == customerId && x.CompanyId == companyId, ct);

}
