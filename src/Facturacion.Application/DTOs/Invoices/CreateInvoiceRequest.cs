namespace Facturacion.Application.DTOs.Invoices;
public sealed record CreateInvoiceRequest(
    Guid CompanyId,
    Guid CustomerId,
    DateOnly IssueDate,
    string Currency,
    IReadOnlyList<CreateInvoiceLineRequest> Lines);
public sealed record CreateInvoiceLineRequest(
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    decimal VatRate);
