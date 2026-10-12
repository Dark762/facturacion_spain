using Facturacion.Application.Common.Results;
using Facturacion.Application.DTOs.Invoices;

namespace Facturacion.Application.Interfaces;

public interface IInvoiceIssuanceService
{
    /// <summary>
    /// Asigna la numeracion definitiva y marca la factura como emitida dentro de
    /// una unica transaccion. Idempotente por diseno: reintentar sobre una factura
    /// ya emitida devuelve el mismo resultado sin duplicar la numeracion.
    /// </summary>
    Task<Result<InvoiceIssueResult>> IssueAsync(
        Guid invoiceId, string series, CancellationToken ct);
}