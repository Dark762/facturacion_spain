namespace Facturacion.Domain.Entities;
public sealed class CompanyUser
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid CompanyId { get; set; }
    public string UserId { get; set; } = "";
    public string Role { get; set; } = "Reader";
    public bool IsActive { get; set; } = true;
}
