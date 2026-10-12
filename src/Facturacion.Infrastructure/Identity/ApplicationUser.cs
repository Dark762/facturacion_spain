using Microsoft.AspNetCore.Identity;
namespace Facturacion.Infrastructure.Identity;
public sealed class ApplicationUser : IdentityUser
{
    public string DisplayName { get; set; } = "";
    public bool IsActive { get; set; } = true;

    // IdentityUser lo declara en false, y UserManager solo aplica el bloqueo
    // por intentos fallidos si el usuario lo tiene activo. Se fuerza aqui para
    // que ningun alta pueda desactivarlo por descuido.
    public override bool LockoutEnabled
    {
        get => true;
        set { }
    }
}
