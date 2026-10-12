using Facturacion.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace Facturacion.Infrastructure.Persistence.Configurations;
public sealed class FiscalRecordConfiguration : IEntityTypeConfiguration<FiscalRecord>
{
    public void Configure(EntityTypeBuilder<FiscalRecord> e)
    {
        e.HasKey(x => x.Id);
        e.HasAlternateKey(x => new { x.TenantId, x.Id });
        e.HasOne(x => x.Invoice).WithMany(x => x.FiscalRecords).HasForeignKey(x => new { x.TenantId, x.InvoiceId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        e.Property(x => x.Hash).HasMaxLength(64);
        e.Property(x => x.PreviousHash).HasMaxLength(64);
        e.Property(x => x.XmlPayload).HasColumnType("longtext");
            e.Property(x => x.InstallationId).HasMaxLength(100);
            e.Property(x => x.SoftwareName).HasMaxLength(200);
            e.Property(x => x.SoftwareVersion).HasMaxLength(50);
    }
}
