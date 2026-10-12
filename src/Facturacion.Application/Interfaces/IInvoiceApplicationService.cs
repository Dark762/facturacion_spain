using Facturacion.Application.Common.Results;
using Facturacion.Application.DTOs.Invoices;
namespace Facturacion.Application.Interfaces;
public interface IInvoiceApplicationService
{
    Task<Result<InvoiceDto>> CreateDraftAsync(CreateInvoiceRequest request, CancellationToken ct);
    Task<Result<InvoiceDto>> GetByIdAsync(Guid invoiceId, CancellationToken ct);
}
