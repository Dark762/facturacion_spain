using System.Security.Claims;
using Facturacion.Application.Invitations;
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
        if (!Guid.TryParse(User.FindFirstValue("tenant_id"), out var tenantId)) return Forbid();
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();
        try { return Ok(await _service.InviteAsync(tenantId, userId, request, ct)); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    [HttpPost("accept")]
    [AllowAnonymous]
    public async Task<IActionResult> Accept([FromBody] AcceptInvitationRequest request, CancellationToken ct)
    {
        try
        {
            await _service.AcceptAsync(request.Token, request.Password, request.DisplayName,
                User.Identity?.IsAuthenticated == true ? User.FindFirstValue(ClaimTypes.NameIdentifier) : null, ct);
            return NoContent();
        }
        catch (UnauthorizedAccessException) { return Unauthorized(new { message = "Inicia sesión con el correo invitado." }); }
    }
}
