using Facturacion.Domain.Interfaces;
using Facturacion.Domain.Enums;
namespace Facturacion.Domain.Entities;
public sealed class AeatSubmission : ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid FiscalRecordId { get; set; }
    public string Environment { get; set; } = "Test";
    public DateTimeOffset SubmittedAt { get; set; }
    public SubmissionStatus Status { get; set; } = SubmissionStatus.Pending;
    public string? Csv { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorDescription { get; set; }
    public string RequestXml { get; set; } = "";
    public string? ResponseXml { get; set; }
    public FiscalRecord FiscalRecord { get; set; } = null!;
}
