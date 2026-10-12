namespace Facturacion.Application.DTOs.Fiscal;
public sealed record AeatSubmissionResult(Guid FiscalRecordId, string Status, string? Csv, string? ErrorCode);
