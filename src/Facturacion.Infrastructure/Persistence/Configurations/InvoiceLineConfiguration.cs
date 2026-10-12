using Facturacion.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace Facturacion.Infrastructure.Persistence.Configurations;
public sealed class InvoiceLineConfiguration : IEntityTypeConfiguration<InvoiceLine>
{
    public void Configure(EntityTypeBuilder<InvoiceLine> e)
    {
        e.HasKey(x => x.Id);
        e.HasAlternateKey(x => new { x.TenantId, x.Id });
        e.HasOne(x => x.Invoice).WithMany(x => x.Lines).HasForeignKey(x => new { x.TenantId, x.InvoiceId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        e.Property(x => x.Quantity).HasPrecision(18,6);
        e.Property(x => x.UnitPrice).HasPrecision(18,6);
        e.Property(x => x.DiscountPercent).HasPrecision(9,4);
        e.Property(x => x.VatRate).HasPrecision(9,4);
        e.Property(x => x.NetAmount).HasPrecision(18,2);
        e.Property(x => x.VatAmount).HasPrecision(18,2);
        e.Property(x => x.TotalAmount).HasPrecision(18,2);
    }
}
