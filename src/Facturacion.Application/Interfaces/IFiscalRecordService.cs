using Facturacion.Application.Common.Results;
using Facturacion.Domain.Entities;
namespace Facturacion.Application.Interfaces;
public interface IFiscalRecordService
{
    Task<Result<Guid>> CreateAltaAsync(Invoice invoice, CancellationToken ct);
}
