using Facturacion.Application.Common.Results;
using Facturacion.Application.DTOs.B2B;
namespace Facturacion.Application.Interfaces;
public interface IB2BProvider
{
    Task<Result<B2BSubmissionResult>> SendAsync(byte[] invoiceXml, CancellationToken ct);
}
