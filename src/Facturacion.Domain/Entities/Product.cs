using Facturacion.Domain.Interfaces;

namespace Facturacion.Domain.Entities;

/// <summary>Artículo o servicio del catálogo de una empresa; nunca se comparte entre tenants.</summary>
public sealed class Product : ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public decimal UnitPrice { get; set; }
    public decimal VatRate { get; set; }
    public bool IsActive { get; set; } = true;
    public Company Company { get; set; } = null!;
}
