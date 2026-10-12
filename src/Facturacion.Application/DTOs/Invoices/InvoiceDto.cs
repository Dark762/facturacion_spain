namespace Facturacion.Application.DTOs.Invoices;
public sealed record InvoiceDto(
    Guid Id, Guid CompanyId, Guid CustomerId,
    string? Series, string? Number,
    decimal TaxableBase, decimal TaxAmount,
    decimal Total, string Status);
