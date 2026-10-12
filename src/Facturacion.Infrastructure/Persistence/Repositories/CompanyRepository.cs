using Facturacion.Application.Interfaces;
using Facturacion.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Facturacion.Infrastructure.Persistence.Repositories;

public sealed class CompanyRepository : ICompanyRepository
{
    private readonly BillingDbContext _db;
    public CompanyRepository(BillingDbContext db) => _db = db;

    public Task<Company?> GetByIdAsync(Guid id, CancellationToken ct) =>
        _db.Companys.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task AddAsync(Company entity, CancellationToken ct) =>
        await _db.Companys.AddAsync(entity, ct);

    public async Task<IReadOnlyList<Company>> GetAllAsync(CancellationToken ct) =>
        await _db.Companys.AsNoTracking().OrderBy(x => x.LegalName).ToListAsync(ct);

}
