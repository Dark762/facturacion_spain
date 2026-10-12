namespace Facturacion.Application.DTOs.B2B;
public sealed record B2BSubmissionResult(bool Accepted, string? ExternalId, string? ErrorCode, string? ErrorDescription);
