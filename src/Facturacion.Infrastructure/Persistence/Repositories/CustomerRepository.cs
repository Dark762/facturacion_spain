using Facturacion.Application.Interfaces;
using Facturacion.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Facturacion.Infrastructure.Persistence.Repositories;

public sealed class CustomerRepository : ICustomerRepository
{
    private readonly BillingDbContext _db;
    public CustomerRepository(BillingDbContext db) => _db = db;

    public Task<Customer?> GetByIdAsync(Guid id, CancellationToken ct) =>
        _db.Customers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task AddAsync(Customer entity, CancellationToken ct) =>
        await _db.Customers.AddAsync(entity, ct);

    public async Task<IReadOnlyList<Customer>> GetByCompanyAsync(Guid companyId, CancellationToken ct) =>
        await _db.Customers.AsNoTracking().Where(x => x.CompanyId == companyId).ToListAsync(ct);

}
