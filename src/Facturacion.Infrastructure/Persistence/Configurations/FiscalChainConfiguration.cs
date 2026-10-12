using Facturacion.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace Facturacion.Infrastructure.Persistence.Configurations;
public sealed class FiscalChainConfiguration : IEntityTypeConfiguration<FiscalChain>
{
    public void Configure(EntityTypeBuilder<FiscalChain> e)
    {
        e.HasKey(x => x.Id);
        e.HasAlternateKey(x => new { x.TenantId, x.Id });
        e.HasOne(x => x.Company).WithMany().HasForeignKey(x => new { x.TenantId, x.CompanyId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        e.HasIndex(x => new { x.TenantId, x.CompanyId, x.InstallationId }).IsUnique();
            e.Property(x => x.InstallationId).HasMaxLength(100);
            e.Property(x => x.LastHash).HasMaxLength(64);
    }
}
