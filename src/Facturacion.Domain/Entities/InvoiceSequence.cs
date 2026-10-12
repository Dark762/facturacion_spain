using Facturacion.Domain.Interfaces;
using Facturacion.Domain.Enums;
namespace Facturacion.Domain.Entities;
public sealed class InvoiceSequence : ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public string Series { get; set; } = "";
    public int FiscalYear { get; set; }
    public long NextNumber { get; set; } = 1;
    public Company Company { get; set; } = null!;
}
