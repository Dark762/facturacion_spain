using Facturacion.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace Facturacion.Infrastructure.Persistence.Configurations;
public sealed class AeatSubmissionConfiguration : IEntityTypeConfiguration<AeatSubmission>
{
    public void Configure(EntityTypeBuilder<AeatSubmission> e)
    {
        e.HasKey(x => x.Id);
        e.HasAlternateKey(x => new { x.TenantId, x.Id });
        e.HasOne(x => x.FiscalRecord).WithMany(x => x.Submissions).HasForeignKey(x => new { x.TenantId, x.FiscalRecordId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        e.Property(x => x.RequestXml).HasColumnType("longtext");
        e.Property(x => x.ResponseXml).HasColumnType("longtext");
    }
}
