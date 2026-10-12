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
    }
}
