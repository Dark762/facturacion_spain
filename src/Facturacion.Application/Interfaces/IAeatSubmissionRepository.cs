using Facturacion.Domain.Entities;
namespace Facturacion.Application.Interfaces;
public interface IAeatSubmissionRepository
{
    Task<IReadOnlyList<AeatSubmission>> GetByFiscalRecordAsync(Guid fiscalRecordId, CancellationToken ct);
    Task<AeatSubmission?> GetByIdAsync(Guid id, CancellationToken ct);
    Task AddAsync(AeatSubmission entity, CancellationToken ct);
}
