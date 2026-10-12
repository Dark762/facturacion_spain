using Facturacion.Application.Common.Results;
using Facturacion.Application.DTOs.Fiscal;
namespace Facturacion.Application.Interfaces;
public interface IAeatSubmissionService
{
    Task<Result<AeatSubmissionResult>> SubmitAsync(Guid fiscalRecordId, CancellationToken ct);
}
