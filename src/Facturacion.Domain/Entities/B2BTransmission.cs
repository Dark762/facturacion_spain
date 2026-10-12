using Facturacion.Domain.Interfaces;
using Facturacion.Domain.Enums;
namespace Facturacion.Domain.Entities;
public sealed class B2BTransmission : ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ElectronicDocumentId { get; set; }
    public string Provider { get; set; } = "";
    public string? ExternalId { get; set; }
    public SubmissionStatus Status { get; set; } = SubmissionStatus.Pending;
    public DateTimeOffset? SubmittedAt { get; set; }
    public DateTimeOffset? AcknowledgedAt { get; set; }
    public string? RequestPayload { get; set; }
    public string? ResponsePayload { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorDescription { get; set; }
    public ElectronicDocument ElectronicDocument { get; set; } = null!;
}
