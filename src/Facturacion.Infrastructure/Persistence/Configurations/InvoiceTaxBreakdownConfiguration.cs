using Facturacion.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace Facturacion.Infrastructure.Persistence.Configurations;
public sealed class InvoiceTaxBreakdownConfiguration : IEntityTypeConfiguration<InvoiceTaxBreakdown>
{
    public void Configure(EntityTypeBuilder<InvoiceTaxBreakdown> e)
    {
        e.HasKey(x => x.Id);
        e.HasAlternateKey(x => new { x.TenantId, x.Id });
        e.HasOne(x => x.Invoice).WithMany(x => x.TaxBreakdowns).HasForeignKey(x => new { x.TenantId, x.InvoiceId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        e.Property(x => x.TaxableBase).HasPrecision(18,2);
        e.Property(x => x.TaxAmount).HasPrecision(18,2);
        e.Property(x => x.SurchargeAmount).HasPrecision(18,2);
        e.Property(x => x.TaxRate).HasPrecision(9,4);
        e.Property(x => x.SurchargeRate).HasPrecision(9,4);
            e.Property(x => x.ExemptionCode).HasMaxLength(30);
            e.Property(x => x.OperationClassification).HasMaxLength(60);
            e.Property(x => x.RegimeKey).HasMaxLength(20);
            e.Property(x => x.TaxCode).HasMaxLength(20);
    }
}
