using Facturacion.Domain.Interfaces;
using Facturacion.Domain.Enums;
namespace Facturacion.Domain.Entities;
public sealed class Invoice : ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public Guid CustomerId { get; set; }
    public string? Series { get; set; }
    public string? Number { get; set; }
    public InvoiceType Type { get; set; } = InvoiceType.F1;
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;
    public DateOnly IssueDate { get; set; }
    public DateOnly? OperationDate { get; set; }
    public DateOnly? DueDate { get; set; }
    public string Currency { get; set; } = "EUR";
    public decimal TaxableBase { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal WithholdingAmount { get; set; }
    public decimal Total { get; set; }
    public string IssuerNif { get; set; } = "";
    public string IssuerName { get; set; } = "";
    public string CustomerTaxId { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public Guid? CorrectedInvoiceId { get; set; }
    public DateTimeOffset? IssuedAt { get; set; }
    public Company Company { get; set; } = null!;
    public Customer Customer { get; set; } = null!;
    public ICollection<InvoiceLine> Lines { get; set; } = new List<InvoiceLine>();
    public ICollection<InvoiceTaxBreakdown> TaxBreakdowns { get; set; } = new List<InvoiceTaxBreakdown>();
    public ICollection<FiscalRecord> FiscalRecords { get; set; } = new List<FiscalRecord>();
    public ICollection<InvoicePayment> Payments { get; set; } = new List<InvoicePayment>();
}
