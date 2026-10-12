using Facturacion.Application.Interfaces;
using Facturacion.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Facturacion.Infrastructure.Persistence.Repositories;

public sealed class AeatSubmissionRepository : IAeatSubmissionRepository
{
    private readonly BillingDbContext _db;
    public AeatSubmissionRepository(BillingDbContext db) => _db = db;

    public Task<AeatSubmission?> GetByIdAsync(Guid id, CancellationToken ct) =>
        _db.AeatSubmissions.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task AddAsync(AeatSubmission entity, CancellationToken ct) =>
        await _db.AeatSubmissions.AddAsync(entity, ct);

    public async Task<IReadOnlyList<AeatSubmission>> GetByFiscalRecordAsync(Guid fiscalRecordId, CancellationToken ct) =>
        await _db.AeatSubmissions.AsNoTracking()
            .Where(x => x.FiscalRecordId == fiscalRecordId)
            .OrderBy(x => x.SubmittedAt).ToListAsync(ct);

}
