namespace Facturacion.Infrastructure.Billing;

/// <summary>Solicitud de verificación de identidad y autorización para actuar por una entidad fiscal.
/// Nunca almacenar imágenes de DNI, certificados privados ni documentos sin cifrado y política de retención.</summary>
public sealed class IdentityVerification
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = "";
    public Guid TenantId { get; set; }
    public string Method { get; set; } = "Manual";
    public string Status { get; set; } = "Pending";
    public string SubjectKind { get; set; } = "Individual";
    public string? EvidenceReference { get; set; }
    public string? CertificateThumbprint { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ReviewedAt { get; set; }
    public string? ReviewedBy { get; set; }
    public string? DecisionCode { get; set; }
}
