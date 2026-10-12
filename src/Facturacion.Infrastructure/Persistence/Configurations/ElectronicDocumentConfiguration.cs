using Facturacion.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace Facturacion.Infrastructure.Persistence.Configurations;
public sealed class ElectronicDocumentConfiguration : IEntityTypeConfiguration<ElectronicDocument>
{
    public void Configure(EntityTypeBuilder<ElectronicDocument> e)
    {
        e.HasKey(x => x.Id);
        e.HasAlternateKey(x => new { x.TenantId, x.Id });
        e.HasOne(x => x.Invoice).WithMany().HasForeignKey(x => new { x.TenantId, x.InvoiceId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        e.Property(x => x.XmlPayload).HasColumnType("longtext");
            e.Property(x => x.ContentHash).HasMaxLength(128);
            e.Property(x => x.DocumentNumber).HasMaxLength(80);
            e.Property(x => x.Format).HasMaxLength(50);
            e.Property(x => x.FormatVersion).HasMaxLength(30);
    }
}
