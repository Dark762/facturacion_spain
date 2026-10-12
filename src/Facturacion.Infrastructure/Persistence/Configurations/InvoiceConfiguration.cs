using Facturacion.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace Facturacion.Infrastructure.Persistence.Configurations;
public sealed class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> e)
    {
        e.HasKey(x => x.Id);
        e.HasAlternateKey(x => new { x.TenantId, x.Id });
        e.HasOne(x => x.Company).WithMany(x => x.Invoices).HasForeignKey(x => new { x.TenantId, x.CompanyId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        e.HasOne(x => x.Customer).WithMany().HasForeignKey(x => new { x.TenantId, x.CustomerId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        // MySQL trata cada NULL como distinto en un indice UNIQUE, de modo que
        // los borradores sin numerar (Series/Number = NULL) no colisionan entre si.
        e.HasIndex(x => new { x.TenantId, x.CompanyId, x.Series, x.Number }).IsUnique();
        e.Property(x => x.Total).HasPrecision(18,2);
        e.Property(x => x.TaxableBase).HasPrecision(18,2);
        e.Property(x => x.TaxAmount).HasPrecision(18,2);
        e.Property(x => x.WithholdingAmount).HasPrecision(18,2);
            e.Property(x => x.Currency).HasMaxLength(3);
            e.Property(x => x.CustomerName).HasMaxLength(250);
            e.Property(x => x.CustomerTaxId).HasMaxLength(20);
            e.Property(x => x.IssuerName).HasMaxLength(250);
            e.Property(x => x.IssuerNif).HasMaxLength(20);
            e.Property(x => x.Number).HasMaxLength(40);
            e.Property(x => x.Series).HasMaxLength(32);
    }
}
