using Facturacion.Domain.Interfaces;
using Facturacion.Domain.Enums;
namespace Facturacion.Domain.Entities;
public sealed class Company : ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Nif { get; set; } = "";
    public string LegalName { get; set; } = "";
    public string? TradeName { get; set; }
    public string Address { get; set; } = "";
    public string PostalCode { get; set; } = "";
    public string City { get; set; } = "";
    public string Province { get; set; } = "";
    public string CountryCode { get; set; } = "ES";
    public string? Email { get; set; }
    public bool IsActive { get; set; } = true;
    public Tenant Tenant { get; set; } = null!;
    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
}
