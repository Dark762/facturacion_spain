using Facturacion.Domain.Interfaces;
using Facturacion.Domain.Enums;
namespace Facturacion.Domain.Entities;
public sealed class Customer : ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public string TaxId { get; set; } = "";
    public string LegalName { get; set; } = "";
    public string Address { get; set; } = "";
    public string PostalCode { get; set; } = "";
    public string City { get; set; } = "";
    public string CountryCode { get; set; } = "ES";
    public string? Email { get; set; }
    public bool IsBusiness { get; set; } = true;
    public Company Company { get; set; } = null!;
}
