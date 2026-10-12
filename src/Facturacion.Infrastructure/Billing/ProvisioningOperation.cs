using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Facturacion.Infrastructure.Billing;

/// <summary>
/// Anchor de idempotencia del aprovisionamiento. La clave primaria es
/// RegistrationId, de modo que el primer INSERT que llega gana el derecho a
/// crear el tenant y los reintentos solo reanudan esa misma operacion.
/// </summary>
public sealed class ProvisioningOperation
{
    public Guid RegistrationId { get; set; }
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public string Status { get; set; } = "Claimed";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>No se persiste: indica si este procesoNonetheless gano el claim.</summary>
    [NotMapped]
    public bool OwnedByCaller { get; set; }
}

public static class ProvisioningOperationStates
{
    public const string Claimed = "Claimed";
    public const string Completed = "Completed";
}