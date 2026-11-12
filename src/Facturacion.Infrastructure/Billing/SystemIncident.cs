namespace Facturacion.Infrastructure.Billing;

/// <summary>Incidencia operativa global; nunca almacenar secretos ni texto bruto de excepciones.</summary>
public sealed class SystemIncident
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string Application { get; set; } = "Facturacion.Api";
    public string Operation { get; set; } = "";
    public string Severity { get; set; } = "Error";
    public string ErrorCode { get; set; } = "INTERNAL_ERROR";
    public string Status { get; set; } = "Open";
    public string? TraceId { get; set; }
    public Guid? TenantId { get; set; }
    public int Occurrences { get; set; } = 1;
    public string? ResolvedBy { get; set; }
}
