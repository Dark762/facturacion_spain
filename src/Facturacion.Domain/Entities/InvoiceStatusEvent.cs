using Facturacion.Domain.Interfaces;
using Facturacion.Domain.Enums;
namespace Facturacion.Domain.Entities;
public sealed class InvoiceStatusEvent : ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid InvoiceId { get; set; }
    public string EventType { get; set; } = "";
    public string Source { get; set; } = "";
    public DateTimeOffset OccurredAt { get; set; }
    public DateTimeOffset? ReportedAt { get; set; }
    public string? ExternalReference { get; set; }
    public string? Payload { get; set; }
    public Invoice Invoice { get; set; } = null!;
}
