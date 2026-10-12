using Facturacion.Application.Interfaces;
using Facturacion.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Facturacion.Infrastructure.Persistence.Repositories;

public sealed class FiscalChainRepository : IFiscalChainRepository
{
    private readonly BillingDbContext _db;
    public FiscalChainRepository(BillingDbContext db) => _db = db;

    public Task<FiscalChain?> GetByIdAsync(Guid id, CancellationToken ct) =>
        _db.FiscalChains.FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<FiscalChain?> GetByCompanyAsync(Guid tenantId, Guid companyId, CancellationToken ct) =>
        _db.FiscalChains.FirstOrDefaultAsync(
            x => x.TenantId == tenantId && x.CompanyId == companyId, ct);

    public async Task AddAsync(FiscalChain entity, CancellationToken ct) =>
        await _db.FiscalChains.AddAsync(entity, ct);

}
