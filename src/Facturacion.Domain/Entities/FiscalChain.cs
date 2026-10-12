using Facturacion.Domain.Interfaces;
using Facturacion.Domain.Enums;
namespace Facturacion.Domain.Entities;
public sealed class FiscalChain : ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public string InstallationId { get; set; } = "";
    public Guid? LastRecordId { get; set; }
    public string LastHash { get; set; } = "";
    public DateTimeOffset UpdatedAt { get; set; }
    public Company Company { get; set; } = null!;
}
