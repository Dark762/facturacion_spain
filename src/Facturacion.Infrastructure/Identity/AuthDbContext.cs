using Facturacion.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
namespace Facturacion.Infrastructure.Identity;
public sealed class AuthDbContext : IdentityDbContext<ApplicationUser>
{
    public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options) { }
    public DbSet<TenantUser> TenantUsers => Set<TenantUser>();
    public DbSet<CompanyUser> CompanyUsers => Set<CompanyUser>();
    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);
        b.Entity<TenantUser>(e => {
            e.ToTable("TenantUsers"); e.HasKey(x => x.Id);
            e.Property(x => x.UserId).HasMaxLength(450).IsRequired();
            e.HasIndex(x => new { x.TenantId, x.UserId }).IsUnique();
            e.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        });
        b.Entity<CompanyUser>(e => {
            e.ToTable("CompanyUsers"); e.HasKey(x => x.Id);
            e.Property(x => x.UserId).HasMaxLength(450).IsRequired();
            e.Property(x => x.Role).HasMaxLength(32).IsRequired();
            e.HasIndex(x => new { x.TenantId, x.CompanyId, x.UserId }).IsUnique();
            e.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
