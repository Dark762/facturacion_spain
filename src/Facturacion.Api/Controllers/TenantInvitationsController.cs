using System.Security.Claims;
using Facturacion.Api.Responses;
using Facturacion.Application.Invitations;
using Facturacion.Configuration.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Facturacion.Api.Controllers;

[ApiController]
[Route("api/invitations")]
public sealed class TenantInvitationsController : ControllerBase
{
    private readonly IUserInvitationService _service;
    public TenantInvitationsController(IUserInvitationService service) => _service = service;

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Invite([FromBody] InviteTenantUserRequest request, CancellationToken ct)
    {
        if (!Guid.TryParse(User.FindFirstValue(FacturacionClaim.TenantId.ToClaimType()), out var tenantId)) return this.ApiFailure<object>(403, "FORBIDDEN", "No tienes permisos para esta operación.");
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId)) return this.ApiFailure<object>(401, "UNAUTHORIZED", "Debes iniciar sesión.");
        try { return this.ApiOk(await _service.InviteAsync(tenantId, userId, request, ct), "Invitación enviada correctamente."); }
        catch (UnauthorizedAccessException) { return this.ApiFailure<object>(403, "FORBIDDEN", "No tienes permisos para esta operación."); }
    }

    [HttpPost("accept")]
    [AllowAnonymous]
    public async Task<IActionResult> Accept([FromBody] AcceptInvitationRequest request, CancellationToken ct)
    {
        try
        {
            await _service.AcceptAsync(request.Token, request.Password, request.DisplayName,
                User.Identity?.IsAuthenticated == true ? User.FindFirstValue(ClaimTypes.NameIdentifier) : null, ct);
            return this.ApiOk<object?>(null, "Invitación aceptada correctamente.");
        }
        catch (UnauthorizedAccessException) { return this.ApiFailure<object>(401, "INVITATION_LOGIN_REQUIRED", "Inicia sesión con el correo invitado."); }
    }
}
