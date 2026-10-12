using Facturacion.Domain.Interfaces;
using Facturacion.Domain.Enums;
namespace Facturacion.Domain.Entities;
public sealed class ElectronicDocument : ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public Guid? InvoiceId { get; set; }
    public ElectronicDocumentDirection Direction { get; set; }
    public string Format { get; set; } = "UBL";
    public string FormatVersion { get; set; } = "";
    public string DocumentNumber { get; set; } = "";
    public string XmlPayload { get; set; } = "";
    public string ContentHash { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
    public Invoice? Invoice { get; set; }
    public ICollection<B2BTransmission> Transmissions { get; set; } = new List<B2BTransmission>();
}
