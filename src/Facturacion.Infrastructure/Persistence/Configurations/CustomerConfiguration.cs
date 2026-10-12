using Facturacion.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace Facturacion.Infrastructure.Persistence.Configurations;
public sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> e)
    {
        e.HasKey(x => x.Id);
        e.HasAlternateKey(x => new { x.TenantId, x.Id });
        e.HasOne(x => x.Company).WithMany().HasForeignKey(x => new { x.TenantId, x.CompanyId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        e.HasIndex(x => new { x.TenantId, x.CompanyId, x.TaxId });
            e.Property(x => x.Address).HasMaxLength(500);
            e.Property(x => x.City).HasMaxLength(120);
            e.Property(x => x.CountryCode).HasMaxLength(2);
            e.Property(x => x.Email).HasMaxLength(254);
            e.Property(x => x.LegalName).HasMaxLength(250);
            e.Property(x => x.PostalCode).HasMaxLength(16);
            e.Property(x => x.TaxId).HasMaxLength(20);
    }
}
