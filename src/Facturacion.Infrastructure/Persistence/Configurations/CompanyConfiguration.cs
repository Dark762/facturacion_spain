using Facturacion.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace Facturacion.Infrastructure.Persistence.Configurations;
public sealed class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> e)
    {
        e.HasKey(x => x.Id);
        e.HasAlternateKey(x => new { x.TenantId, x.Id });
        e.HasOne(x => x.Tenant).WithMany(x => x.Companies).HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        e.HasIndex(x => new { x.TenantId, x.Nif }).IsUnique();
            e.Property(x => x.Address).HasMaxLength(500);
            e.Property(x => x.City).HasMaxLength(120);
            e.Property(x => x.CountryCode).HasMaxLength(2);
            e.Property(x => x.Email).HasMaxLength(254);
            e.Property(x => x.LegalName).HasMaxLength(250);
            e.Property(x => x.Nif).HasMaxLength(20);
            e.Property(x => x.PostalCode).HasMaxLength(16);
            e.Property(x => x.Province).HasMaxLength(120);
            e.Property(x => x.TradeName).HasMaxLength(250);
    }
}
