using Facturacion.Application.Interfaces;
using Facturacion.Domain.Entities;
using Facturacion.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Facturacion.Infrastructure.Persistence;
public sealed class BillingDbContext : DbContext
{
    private readonly ITenantContext _tenantContext;
    public Guid TenantId => _tenantContext.TenantId;
    public BillingDbContext(DbContextOptions<BillingDbContext> options, ITenantContext tenantContext) : base(options)
    {
        _tenantContext = tenantContext;
    }
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Company> Companys => Set<Company>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceLine> InvoiceLines => Set<InvoiceLine>();
    public DbSet<InvoiceTaxBreakdown> InvoiceTaxBreakdowns => Set<InvoiceTaxBreakdown>();
    public DbSet<InvoiceSequence> InvoiceSequences => Set<InvoiceSequence>();
    public DbSet<FiscalChain> FiscalChains => Set<FiscalChain>();
    public DbSet<FiscalRecord> FiscalRecords => Set<FiscalRecord>();
    public DbSet<AeatSubmission> AeatSubmissions => Set<AeatSubmission>();
    public DbSet<ElectronicDocument> ElectronicDocuments => Set<ElectronicDocument>();
    public DbSet<B2BTransmission> B2BTransmissions => Set<B2BTransmission>();
    public DbSet<InvoiceStatusEvent> InvoiceStatusEvents => Set<InvoiceStatusEvent>();
    public DbSet<InvoicePayment> InvoicePayments => Set<InvoicePayment>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BillingDbContext).Assembly);
        modelBuilder.Entity<Company>().HasQueryFilter(x => x.TenantId == TenantId);
        modelBuilder.Entity<Customer>().HasQueryFilter(x => x.TenantId == TenantId);
        modelBuilder.Entity<Invoice>().HasQueryFilter(x => x.TenantId == TenantId);
        modelBuilder.Entity<InvoiceLine>().HasQueryFilter(x => x.TenantId == TenantId);
        modelBuilder.Entity<InvoiceTaxBreakdown>().HasQueryFilter(x => x.TenantId == TenantId);
        modelBuilder.Entity<InvoiceSequence>().HasQueryFilter(x => x.TenantId == TenantId);
        modelBuilder.Entity<FiscalChain>().HasQueryFilter(x => x.TenantId == TenantId);
        modelBuilder.Entity<FiscalRecord>().HasQueryFilter(x => x.TenantId == TenantId);
        modelBuilder.Entity<AeatSubmission>().HasQueryFilter(x => x.TenantId == TenantId);
        modelBuilder.Entity<ElectronicDocument>().HasQueryFilter(x => x.TenantId == TenantId);
        modelBuilder.Entity<B2BTransmission>().HasQueryFilter(x => x.TenantId == TenantId);
        modelBuilder.Entity<InvoiceStatusEvent>().HasQueryFilter(x => x.TenantId == TenantId);
        modelBuilder.Entity<InvoicePayment>().HasQueryFilter(x => x.TenantId == TenantId);
        modelBuilder.Entity<OutboxMessage>().HasQueryFilter(x => x.TenantId == TenantId);
        modelBuilder.Entity<AuditEvent>().HasQueryFilter(x => x.TenantId == TenantId);
    }
    public override int SaveChanges() { ValidateTenant(); return base.SaveChanges(); }
    public override int SaveChanges(bool acceptAllChangesOnSuccess) { ValidateTenant(); return base.SaveChanges(acceptAllChangesOnSuccess); }
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) { ValidateTenant(); return base.SaveChangesAsync(cancellationToken); }
    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default) { ValidateTenant(); return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken); }
    private void ValidateTenant()
    {
        foreach (var entry in ChangeTracker.Entries<ITenantEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity.TenantId == Guid.Empty) entry.Entity.TenantId = TenantId;
                if (entry.Entity.TenantId != TenantId) throw new InvalidOperationException("Tenant no autorizado");
            }
            else if (entry.State is EntityState.Modified or EntityState.Deleted)
            {
                if (entry.Entity.TenantId != TenantId) throw new InvalidOperationException("Tenant no autorizado");
                if (entry.State == EntityState.Modified && !Equals(entry.Property(nameof(ITenantEntity.TenantId)).OriginalValue, entry.Property(nameof(ITenantEntity.TenantId)).CurrentValue))
                    throw new InvalidOperationException("TenantId es inmutable");
            }
        }
    }
}
