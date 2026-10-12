namespace Facturacion.Application.DTOs.Invoices;
public sealed record InvoiceIssueResult(Guid InvoiceId, string InvoiceNumber, Guid FiscalRecordId);
