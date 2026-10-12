using Facturacion.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;

namespace Facturacion.Infrastructure.Invitations;

/// <summary>
/// Mapeo de <see cref="TenantInvitation"/>. Se invoca desde
/// <c>AuthDbContext.OnModelCreating</c>; no sustituye la configuracion existente.
/// </summary>
public static class InvitationModelMapping
{
    public static void Configure(ModelBuilder model)
    {
        model.Entity<TenantInvitation>(e =>
        {
            e.ToTable("tenant_invitations");
            e.HasKey(x => x.Id);
            e.Property(x => x.Email).HasMaxLength(256).IsRequired();
            e.Property(x => x.Role).HasMaxLength(20).IsRequired();
            e.Property(x => x.TokenHash).HasMaxLength(64).IsRequired();
            e.Property(x => x.InvitedByUserId).HasMaxLength(255).IsRequired();
            e.Property(x => x.AcceptedByUserId).HasMaxLength(255);

            // Token de un solo uso: la unicidad impide reutilizar el mismo enlace.
            e.HasIndex(x => x.TokenHash).IsUnique();
            e.HasIndex(x => new { x.TenantId, x.CompanyId });
        });
    }
}