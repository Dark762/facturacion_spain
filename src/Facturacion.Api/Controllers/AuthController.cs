using Facturacion.Api.Security;
using System.Security.Claims;
using Facturacion.Api.Responses;
using Facturacion.Infrastructure.Identity;
using Facturacion.Infrastructure.Persistence;
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
    private readonly IConfiguration _configuration;
    private readonly BillingDbContext _billing;
    public AuthController(UserManager<ApplicationUser> users, SignInManager<ApplicationUser> signIn,
        AuthDbContext auth, JwtTokenService tokens, IConfiguration configuration, BillingDbContext billing)
    { _users=users; _signIn=signIn; _auth=auth; _tokens=tokens; _configuration=configuration; _billing=billing; }   

    public sealed record LoginRequest(string Email, string Password, Guid TenantId);
    public sealed record TokenResponse(string AccessToken, DateTime ExpiresUtc);

    public sealed record StartLoginRequest(string Email, string Password);
    public sealed record TenantChoice(Guid TenantId, string Name);
    public sealed record StartLoginResponse(string SelectionToken, DateTime ExpiresUtc,
        TenantChoice[] Tenants, bool CanAccessPlatform);
    public sealed record CompleteLoginRequest(Guid? TenantId, bool Platform = false);

    // La identidad se valida antes de revelar los tenants del usuario.
    [AllowAnonymous]
    [HttpPost("login/start")]
    public async Task<IActionResult> StartLogin(StartLoginRequest input, CancellationToken ct)
    {
        var user = await _users.FindByEmailAsync(input.Email);
        if (user is null || !user.IsActive)
            return this.ApiFailure<object>(401, "UNAUTHORIZED", "Credenciales no válidas.");
        var check = await _signIn.CheckPasswordSignInAsync(user, input.Password, lockoutOnFailure: true);
        if (!check.Succeeded)
            return this.ApiFailure<object>(401, "UNAUTHORIZED", "Credenciales no válidas.");

        var memberships = await _auth.TenantUsers.AsNoTracking()
            .Where(x => x.UserId == user.Id && x.IsActive)
            .Select(x => x.TenantId).ToListAsync(ct);
        var tenants = await _billing.Tenants.AsNoTracking()
            .Where(x => memberships.Contains(x.Id) && x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new TenantChoice(x.Id, x.Name)).ToArrayAsync(ct);
        var platform = string.Equals(user.Id, _configuration["PlatformAdmin:UserId"],
            StringComparison.Ordinal) && !string.IsNullOrWhiteSpace(_configuration["PlatformAdmin:UserId"]);
        if (tenants.Length == 0 && !platform)
            return this.ApiFailure<object>(403, "NO_TENANTS", "No tienes organizaciones activas.");
        var selection = _tokens.CreateSelection(user);
        return this.ApiOk(new StartLoginResponse(selection.Token, selection.ExpiresUtc, tenants, platform));
    }

    // Requiere un JWT de selección firmado y de cinco minutos de vida.
    [Authorize]
    [HttpPost("login/complete")]
    public async Task<IActionResult> CompleteLogin(CompleteLoginRequest input, CancellationToken ct)
    {
        if (User.FindFirst("purpose")?.Value != "tenant_selection")
            return this.ApiFailure<object>(403, "INVALID_TOKEN_PURPOSE", "Sesión de selección no válida.");
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = userId is null ? null : await _users.FindByIdAsync(userId);
        if (user is null || !user.IsActive)
            return this.ApiFailure<object>(401, "UNAUTHORIZED", "Sesión caducada.");
        if (input.Platform)
        {
            if (input.TenantId.HasValue ||
                string.IsNullOrWhiteSpace(_configuration["PlatformAdmin:UserId"]) ||
                !string.Equals(user.Id, _configuration["PlatformAdmin:UserId"], StringComparison.Ordinal))
                return this.ApiFailure<object>(403, "FORBIDDEN", "Acceso a plataforma denegado.");
            var platformToken = _tokens.CreatePlatform(user);
            return this.ApiOk(new TokenResponse(platformToken.Token, platformToken.ExpiresUtc));
        }
        if (!input.TenantId.HasValue)
            return this.ApiFailure<object>(400, "TENANT_REQUIRED", "Selecciona una organización.");
        var tenantId = input.TenantId.Value;
        var valid = await _auth.TenantUsers.AsNoTracking()
            .AnyAsync(x => x.UserId == user.Id && x.TenantId == tenantId && x.IsActive, ct);
        var tenantActive = await _billing.Tenants.AsNoTracking()
            .AnyAsync(x => x.Id == tenantId && x.IsActive, ct);
        if (!valid || !tenantActive)
            return this.ApiFailure<object>(403, "FORBIDDEN", "Organización no autorizada.");
        var token = _tokens.Create(user, tenantId);
        return this.ApiOk(new TokenResponse(token.Token, token.ExpiresUtc));
    }

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
