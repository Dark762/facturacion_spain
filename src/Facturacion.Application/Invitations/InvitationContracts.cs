namespace Facturacion.Application.Invitations;

public sealed record InviteTenantUserRequest(string Email, Guid CompanyId, string Role);
public sealed record AcceptInvitationRequest(string Token, string? Password, string? DisplayName);
public sealed record InvitationCreatedResponse(Guid InvitationId, DateTime ExpiresAtUtc);

public interface IUserInvitationService
{
    Task<InvitationCreatedResponse> InviteAsync(Guid tenantId, string invitedByUserId,
        InviteTenantUserRequest request, CancellationToken ct = default);
    Task AcceptAsync(string token, string? password, string? displayName,
        string? authenticatedUserId, CancellationToken ct = default);
}

public interface IInvitationEmailSender
{
    Task SendInvitationAsync(string email, string invitationUrl, DateTime expiresAtUtc,
        CancellationToken ct = default);
}
