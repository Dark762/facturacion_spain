using System.Security.Cryptography;
using System.Text;
using Facturacion.Application.Invitations;
using Facturacion.Infrastructure.Identity;
using Facturacion.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Facturacion.Infrastructure.Invitations;

public sealed class UserInvitationService : IUserInvitationService
{
    private readonly AuthDbContext _auth;
    private readonly BillingDbContext _billing;
    private readonly UserManager<ApplicationUser> _users;
    private readonly IInvitationEmailSender _email;
    private readonly IConfiguration _config;

    public UserInvitationService(AuthDbContext auth, BillingDbContext billing,
        UserManager<ApplicationUser> users, IInvitationEmailSender email, IConfiguration config)
    {
        _auth = auth; _billing = billing; _users = users; _email = email; _config = config;
    }

    public async Task<InvitationCreatedResponse> InviteAsync(Guid tenantId, string invitedByUserId,
        InviteTenantUserRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || !new System.Net.Mail.MailAddress(request.Email.Trim()).Address.Equals(request.Email.Trim(), StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Correo electrónico inválido.");
        if (request.Role is not ("Reader" or "Editor" or "Admin"))
            throw new ArgumentException("Rol inválido.");
        var authorized = await _auth.TenantUsers.AnyAsync(x => x.TenantId == tenantId && x.UserId == invitedByUserId && x.IsActive, ct)
            && await _auth.CompanyUsers.AnyAsync(x => x.TenantId == tenantId && x.CompanyId == request.CompanyId
                && x.UserId == invitedByUserId && x.IsActive && x.Role == "Admin", ct);
        if (!authorized) throw new UnauthorizedAccessException("No eres administrador de esta empresa.");
        if (!await _billing.Companys.AnyAsync(x => x.TenantId == tenantId && x.Id == request.CompanyId && x.IsActive, ct))
            throw new InvalidOperationException("Empresa inexistente o inactiva.");

        var raw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        var now = DateTime.UtcNow;
        var lifetime = _config.GetValue<int?>("Invitations:ExpirationHours") ?? 48;
        if (lifetime is < 1 or > 720)
            throw new InvalidOperationException("Invitations:ExpirationHours debe estar entre 1 y 720.");

        var invite = new TenantInvitation
        {
            TenantId = tenantId, CompanyId = request.CompanyId, Email = request.Email.Trim().ToLowerInvariant(),
            Role = request.Role, TokenHash = Hash(raw), InvitedByUserId = invitedByUserId,
            CreatedAtUtc = now, ExpiresAtUtc = now.AddHours(lifetime)
        };
        _auth.Set<TenantInvitation>().Add(invite);
        await _auth.SaveChangesAsync(ct);

        var baseUrl = _config["Invitations:AcceptUrl"]
            ?? throw new InvalidOperationException("Configura Invitations:AcceptUrl.");
        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var parsed) || parsed.Scheme != Uri.UriSchemeHttps)
            throw new InvalidOperationException("Invitations:AcceptUrl debe ser una URL HTTPS absoluta.");
        var separator = baseUrl.Contains('?') ? "&" : "?";
        await _email.SendInvitationAsync(invite.Email, baseUrl + separator + "token=" + Uri.EscapeDataString(raw), invite.ExpiresAtUtc, ct);
        return new InvitationCreatedResponse(invite.Id, invite.ExpiresAtUtc);
    }

    public async Task AcceptAsync(string token, string? password, string? displayName,
        string? authenticatedUserId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(token)) throw new ArgumentException("Token obligatorio.");
        var tokenHash = Hash(token);
        // Una transacción de AuthDbContext protege el consumo y las membresías.
        // Se necesita un índice único en TokenHash (véase SQL).
        await using var tx = await _auth.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
        var invitation = await _auth.Set<TenantInvitation>().SingleOrDefaultAsync(x => x.TokenHash == tokenHash, ct)
            ?? throw new InvalidOperationException("Invitación inválida.");
        if (invitation.AcceptedAtUtc is not null || invitation.ExpiresAtUtc <= DateTime.UtcNow)
            throw new InvalidOperationException("Invitación consumida o caducada.");
        if (!await _billing.Companys.AnyAsync(x => x.Id == invitation.CompanyId && x.TenantId == invitation.TenantId && x.IsActive, ct))
            throw new InvalidOperationException("La empresa ya no está disponible.");

        var user = await _users.FindByEmailAsync(invitation.Email);
        if (user is not null)
        {
            // Nunca vincular una cuenta existente solo por poseer el enlace: requiere sesión del titular.
            if (string.IsNullOrWhiteSpace(authenticatedUserId) || authenticatedUserId != user.Id)
                throw new UnauthorizedAccessException("Inicia sesión con la cuenta invitada antes de aceptar.");
            if (!user.IsActive) throw new InvalidOperationException("Usuario inactivo.");
        }
        else
        {
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Debes establecer una contraseña para la nueva cuenta.");
            user = new ApplicationUser
            {
                UserName = invitation.Email, Email = invitation.Email,
                DisplayName = string.IsNullOrWhiteSpace(displayName) ? invitation.Email : displayName.Trim(),
                EmailConfirmed = true, IsActive = true
            };
            var created = await _users.CreateAsync(user, password);
            if (!created.Succeeded)
                throw new InvalidOperationException(string.Join("; ", created.Errors.Select(e => e.Description)));
        }

        var tenantUser = await _auth.TenantUsers.SingleOrDefaultAsync(x => x.TenantId == invitation.TenantId && x.UserId == user.Id, ct);
        if (tenantUser is null)
            _auth.TenantUsers.Add(new Facturacion.Domain.Entities.TenantUser { TenantId = invitation.TenantId, UserId = user.Id });
        else tenantUser.IsActive = true;

        var companyUser = await _auth.CompanyUsers.SingleOrDefaultAsync(x => x.TenantId == invitation.TenantId
            && x.CompanyId == invitation.CompanyId && x.UserId == user.Id, ct);
        if (companyUser is null)
            _auth.CompanyUsers.Add(new Facturacion.Domain.Entities.CompanyUser
                { TenantId = invitation.TenantId, CompanyId = invitation.CompanyId, UserId = user.Id, Role = invitation.Role });
        else { companyUser.IsActive = true; companyUser.Role = invitation.Role; }

        invitation.AcceptedAtUtc = DateTime.UtcNow;
        invitation.AcceptedByUserId = user.Id;
        await _auth.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    private static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
