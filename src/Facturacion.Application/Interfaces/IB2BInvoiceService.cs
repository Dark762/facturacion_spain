using Facturacion.Application.Common.Results;
using Facturacion.Application.DTOs.B2B;
namespace Facturacion.Application.Interfaces;
public interface IB2BInvoiceService
{
    Task<Result<B2BSubmissionResult>> SendAsync(Guid invoiceId, CancellationToken ct);
}
