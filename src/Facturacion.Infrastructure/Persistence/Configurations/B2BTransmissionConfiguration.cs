using Facturacion.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace Facturacion.Infrastructure.Persistence.Configurations;
public sealed class B2BTransmissionConfiguration : IEntityTypeConfiguration<B2BTransmission>
{
    public void Configure(EntityTypeBuilder<B2BTransmission> e)
    {
        e.HasKey(x => x.Id);
        e.HasAlternateKey(x => new { x.TenantId, x.Id });
        e.HasOne(x => x.ElectronicDocument).WithMany(x => x.Transmissions).HasForeignKey(x => new { x.TenantId, x.ElectronicDocumentId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        e.Property(x => x.RequestPayload).HasColumnType("longtext");
        e.Property(x => x.ResponsePayload).HasColumnType("longtext");
    }
}
