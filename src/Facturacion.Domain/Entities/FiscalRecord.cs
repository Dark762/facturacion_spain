using Facturacion.Domain.Interfaces;
using Facturacion.Domain.Enums;
namespace Facturacion.Domain.Entities;
public sealed class FiscalRecord : ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid InvoiceId { get; set; }
    public FiscalRecordType RecordType { get; set; }
    public Guid? PreviousRecordId { get; set; }
    public string PreviousHash { get; set; } = "";
    public string Hash { get; set; } = "";
    public DateTimeOffset GeneratedAt { get; set; }
    public string SoftwareName { get; set; } = "";
    public string SoftwareVersion { get; set; } = "";
    public string InstallationId { get; set; } = "";
    public string XmlPayload { get; set; } = "";
    public SubmissionStatus SubmissionStatus { get; set; } = SubmissionStatus.Pending;
    public Invoice Invoice { get; set; } = null!;
    public ICollection<AeatSubmission> Submissions { get; set; } = new List<AeatSubmission>();
}
