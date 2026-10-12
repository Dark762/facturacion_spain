using Facturacion.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace Facturacion.Infrastructure.Persistence.Configurations;
public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> e)
    {
        e.HasKey(x => x.Id);
        e.HasAlternateKey(x => new { x.TenantId, x.Id });
        e.HasIndex(x => new { x.TenantId, x.Status, x.NextAttemptAt });
        e.Property(x => x.Payload).HasColumnType("longtext");
        e.Property(x => x.LastError).HasColumnType("longtext");
    }
}
