using Facturacion.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Facturacion.Infrastructure.Persistence.Configurations;

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> e)
    {
        e.HasKey(x => x.Id);
        e.HasAlternateKey(x => new { x.TenantId, x.Id });
        e.HasOne(x => x.Company).WithMany()
            .HasForeignKey(x => new { x.TenantId, x.CompanyId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);
        e.HasIndex(x => new { x.TenantId, x.CompanyId, x.Code }).IsUnique();
        e.Property(x => x.Code).HasMaxLength(64).IsRequired();
        e.Property(x => x.Name).HasMaxLength(250).IsRequired();
        e.Property(x => x.Description).HasMaxLength(1000);
        e.Property(x => x.UnitPrice).HasPrecision(18, 2);
        e.Property(x => x.VatRate).HasPrecision(5, 2);
    }
}
