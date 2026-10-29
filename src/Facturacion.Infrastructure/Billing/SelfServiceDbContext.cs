using Facturacion.Application.Billing;
using Microsoft.EntityFrameworkCore;
namespace Facturacion.Infrastructure.Billing;
// Contexto de control comercial SIN filtro tenant: se usa exclusivamente desde servicios internos.
// Mantenerlo en la MISMA base de datos compartida que Identity y Billing.
public sealed class SelfServiceDbContext : DbContext
{
    public SelfServiceDbContext(DbContextOptions<SelfServiceDbContext> options) : base(options) { }
    public DbSet<RegistrationIntent> RegistrationIntents => Set<RegistrationIntent>();
    public DbSet<SubscriptionPlan> SubscriptionPlans => Set<SubscriptionPlan>();
    public DbSet<ProcessedPaymentEvent> ProcessedPaymentEvents => Set<ProcessedPaymentEvent>();
    public DbSet<SubscriptionState> SubscriptionStates => Set<SubscriptionState>();
    public DbSet<ProvisioningOperation> ProvisioningOperations => Set<ProvisioningOperation>();
    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<RegistrationIntent>(x => { x.ToTable("RegistrationIntents"); x.HasKey(y => y.Id); x.HasIndex(y => y.UserId).IsUnique(); x.HasIndex(y => y.CheckoutSessionId).IsUnique(); });
        b.Entity<SubscriptionPlan>(x => { x.ToTable("SubscriptionPlans"); x.HasKey(y => y.Code); x.HasIndex(y => y.StripePriceId).IsUnique(); });
        b.Entity<ProcessedPaymentEvent>(x => { x.ToTable("ProcessedPaymentEvents"); x.HasKey(y => y.Id); });
        b.Entity<SubscriptionState>(x => { x.ToTable("SubscriptionStates"); x.HasKey(y => y.Id); x.HasIndex(y => y.StripeSubscriptionId).IsUnique(); x.HasIndex(y => y.TenantId).IsUnique(); });
        b.Entity<ProvisioningOperation>(x => {
            x.ToTable("ProvisioningOperations");
            // La clave es RegistrationId: es lo que hace atomico el claim.
            x.HasKey(y => y.RegistrationId);
            x.Property(y => y.Status).HasMaxLength(32).IsRequired();
        });
    }
}
public sealed class RegistrationIntent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = "";
    public string TenantName { get; set; } = "";
    public string HolderType { get; set; } = FiscalHolderType.Individual.ToStorageValue();
    public string TradeName { get; set; } = "";
    public string Nif { get; set; } = "";
    public string LegalName { get; set; } = "";
    public string Address { get; set; } = "";
    public string PostalCode { get; set; } = "";
    public string City { get; set; } = "";
    public string Province { get; set; } = "";
    public string PlanCode { get; set; } = "";
    public string? CheckoutSessionId { get; set; }
    public string? CheckoutTokenHash { get; set; }
    public Guid? TenantId { get; set; }
    public string Status { get; set; } = RegistrationStatus.PendingEmail.ToStorageValue();
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
public sealed class SubscriptionPlan
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string StripePriceId { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public decimal MonthlyPrice { get; set; }
    public string Currency { get; set; } = "EUR";
    public int MaxCompanies { get; set; }
    public int MaxUsers { get; set; }
    public int MaxInvoicesPerMonth { get; set; }
    public string Description { get; set; } = "";
    public int DisplayOrder { get; set; }
}
public sealed class ProcessedPaymentEvent
{
    public string Id { get; set; } = "";
    public DateTimeOffset ReceivedAt { get; set; } = DateTimeOffset.UtcNow;
}
public sealed class SubscriptionState
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public string StripeCustomerId { get; set; } = "";
    public string StripeSubscriptionId { get; set; } = "";
    public string PlanCode { get; set; } = "";
    public string Status { get; set; } = "Pending";
}
