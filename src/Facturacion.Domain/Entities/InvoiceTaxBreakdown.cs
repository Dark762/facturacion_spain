using Facturacion.Domain.Interfaces;
using Facturacion.Domain.Enums;
namespace Facturacion.Domain.Entities;
public sealed class InvoiceTaxBreakdown : ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid InvoiceId { get; set; }
    public string TaxCode { get; set; } = "IVA";
    public string? RegimeKey { get; set; }
    public string? OperationClassification { get; set; }
    public string? ExemptionCode { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TaxableBase { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal? SurchargeRate { get; set; }
    public decimal? SurchargeAmount { get; set; }
    public Invoice Invoice { get; set; } = null!;
}
