using Facturacion.Api.Security;
using Facturacion.Api.Responses;
using Facturacion.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace Facturacion.Api.Controllers;
[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly SignInManager<ApplicationUser> _signIn;
    private readonly AuthDbContext _auth;
    private readonly JwtTokenService _tokens;
    public AuthController(UserManager<ApplicationUser> users, SignInManager<ApplicationUser> signIn,
        AuthDbContext auth, JwtTokenService tokens)
    { _users=users; _signIn=signIn; _auth=auth; _tokens=tokens; }   

    public sealed record LoginRequest(string Email, string Password, Guid TenantId);
    public sealed record TokenResponse(string AccessToken, DateTime ExpiresUtc);

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest input, CancellationToken ct)
    {
        var user = await _users.FindByEmailAsync(input.Email);

        if (user is null || !user.IsActive) return this.ApiFailure<object>(401, "UNAUTHORIZED", "Credenciales o acceso al tenant no válidos.");

        var check = await _signIn.CheckPasswordSignInAsync(user, input.Password, lockoutOnFailure: true);

        if (!check.Succeeded) return this.ApiFailure<object>(401, "UNAUTHORIZED", "Credenciales o acceso al tenant no válidos.");

        if (!await _auth.TenantUsers.AsNoTracking().AnyAsync(x => x.TenantId == input.TenantId && x.UserId == user.Id && x.IsActive, ct))
            return this.ApiFailure<object>(401, "UNAUTHORIZED", "Credenciales o acceso al tenant no válidos.");

        var token = _tokens.Create(user, input.TenantId);

        return this.ApiOk(new TokenResponse(token.Token, token.ExpiresUtc), "Sesión iniciada correctamente.");
    }

    [Authorize]
    [HttpGet("me/companies")]
    public async Task<IActionResult> Companies(
        [FromServices] Facturacion.Application.Interfaces.ITenantContext tenant, CancellationToken ct)
    {
        var userId = _users.GetUserId(User);
        var companies = await _auth.CompanyUsers.AsNoTracking()
            .Where(x => x.TenantId == tenant.TenantId && x.UserId == userId && x.IsActive)
            .Select(x => new { x.CompanyId, x.Role }).ToListAsync(ct);
        return this.ApiOk(companies);
    }
}
