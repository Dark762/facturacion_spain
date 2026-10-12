using Facturacion.Application.Invitations;
using Facturacion.Domain.Entities;
using Facturacion.Infrastructure.Identity;
using Facturacion.Infrastructure.Invitations;
using Facturacion.Integration.Test.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Facturacion.Integration.Test;

/// <summary>
/// Flujo de invitacion: alta, consumo unico, caducidad y, sobre todo, que un
/// enlace no pueda vincular una cuenta ajena sin sesion del titular.
/// </summary>
public sealed class TenantInvitationTests : IClassFixture<InvitationFixture>
{
    private readonly InvitationFixture _fixture;

    public TenantInvitationTests(InvitationFixture fixture) => _fixture = fixture;

    private static string NewEmail() => $"invitado-{Guid.NewGuid():N}@example.test";

    private async Task<(Guid Id, string Token, string Email)> InviteAsync(
        string email, string role = "Editor")
    {
        using var scope = _fixture.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IUserInvitationService>();

        var result = await service.InviteAsync(
            InvitationFixture.TenantId, _fixture.Admin.Id,
            new InviteTenantUserRequest(email, _fixture.Company.Id, role),
            CancellationToken.None);

        var sent = Assert.Single(
            _fixture.Emails.Sent, x => x.Email == email.ToLowerInvariant());

        // Extraer el token del enlace tal y como lo recibiria el usuario.
        var query = sent.Url[(sent.Url.IndexOf("token=", StringComparison.Ordinal) + 6)..];
        return (result.InvitationId, Uri.UnescapeDataString(query), email);
    }

    [Fact]
    public async Task Invite_persists_the_row_and_sends_an_https_link()
    {
        var email = NewEmail();
        var (id, token, _) = await InviteAsync(email, "Reader");

        Assert.NotEqual(Guid.Empty, id);
        Assert.True(token.Length >= 40, "El token debe tener entropia suficiente.");

        var sent = Assert.Single(_fixture.Emails.Sent, x => x.Email == email.ToLowerInvariant());
        Assert.StartsWith("https://", sent.Url);

        using var scope = _fixture.CreateScope();
        var auth = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        var invite = await auth.Set<TenantInvitation>().SingleAsync(x => x.Id == id);

        Assert.Equal("Reader", invite.Role);
        Assert.Null(invite.AcceptedAtUtc);
        Assert.NotEqual(token, invite.TokenHash);
    }

    [Fact]
    public async Task Accept_creates_the_account_and_both_memberships()
    {
        var email = NewEmail();
        var (_, token, _) = await InviteAsync(email, "Editor");

        using var scope = _fixture.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IUserInvitationService>();
        await service.AcceptAsync(token, "Aa1!contrasenaLarga", "Invitado", null, CancellationToken.None);

        var auth = scope.ServiceProvider.GetRequiredService<AuthDbContext>();

        var user = Assert.Single(await auth.Users.Where(u => u.Email == email.ToLowerInvariant()).ToListAsync());
        Assert.Equal("Invitado", user.DisplayName);
        Assert.True(await auth.TenantUsers.AnyAsync(
            x => x.TenantId == InvitationFixture.TenantId && x.UserId == user.Id && x.IsActive));
        Assert.True(await auth.CompanyUsers.AnyAsync(
            x => x.TenantId == InvitationFixture.TenantId
                 && x.CompanyId == _fixture.Company.Id
                 && x.UserId == user.Id && x.Role == "Editor" && x.IsActive));
    }

    [Fact]
    public async Task A_token_is_single_use()
    {
        var (_, token, _) = await InviteAsync(NewEmail());

        using (var scope = _fixture.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<IUserInvitationService>();
            await service.AcceptAsync(token, "Aa1!contrasenaLarga", null, null, CancellationToken.None);
        }

        using var again = _fixture.CreateScope();
        var svc = again.ServiceProvider.GetRequiredService<IUserInvitationService>();
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.AcceptAsync(token, "Aa1!contrasenaLarga", null, null, CancellationToken.None));
    }

    [Fact]
    public async Task An_invalid_token_is_rejected()
    {
        await InviteAsync(NewEmail());

        using var scope = _fixture.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IUserInvitationService>();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AcceptAsync("token-que-no-existe", "Aa1!contrasenaLarga", null, null, CancellationToken.None));
    }

    [Fact]
    public async Task An_expired_invitation_is_rejected()
    {
        var email = NewEmail();
        var (id, token, _) = await InviteAsync(email);

        // Envejecer la invitacion directamente en la base, como si hubieran pasado 48 h.
        using (var scope = _fixture.CreateScope())
        {
            var auth = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
            var invite = await auth.Set<TenantInvitation>().SingleAsync(x => x.Id == id);
            invite.ExpiresAtUtc = DateTime.UtcNow.AddMinutes(-1);
            await auth.SaveChangesAsync();
        }

        using var check = _fixture.CreateScope();
        var service = check.ServiceProvider.GetRequiredService<IUserInvitationService>();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AcceptAsync(token, "Aa1!contrasenaLarga", null, null, CancellationToken.None));
    }

    [Fact]
    public async Task An_existing_account_cannot_be_claimed_with_the_link_alone()
    {
        // Regresion de seguridad: poseer el enlace no debe bastar para vincular
        // una cuenta ya existente; se exige sesion del titular.
        var email = NewEmail();

        using (var scope = _fixture.CreateScope())
        {
            var auth = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
            var users = scope.ServiceProvider.GetRequiredService<
                Microsoft.AspNetCore.Identity.UserManager<ApplicationUser>>();
            var existing = InvitationFixture.NewUser(email);
            var created = await users.CreateAsync(existing, "Aa1!contrasenaExistente");
            Assert.True(created.Succeeded);
        }

        var (_, token, _) = await InviteAsync(email);

        using var accept = _fixture.CreateScope();
        var service = accept.ServiceProvider.GetRequiredService<IUserInvitationService>();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.AcceptAsync(token, null, null, null, CancellationToken.None));
    }

    [Fact]
    public async Task The_owner_of_an_existing_account_can_accept_when_authenticated()
    {
        var email = NewEmail();
        string userId;

        using (var scope = _fixture.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<
                Microsoft.AspNetCore.Identity.UserManager<ApplicationUser>>();
            var existing = InvitationFixture.NewUser(email);
            var created = await users.CreateAsync(existing, "Aa1!contrasenaExistente");
            Assert.True(created.Succeeded);
            userId = existing.Id;
        }

        var (_, token, _) = await InviteAsync(email, "Admin");

        using var accept = _fixture.CreateScope();
        var service = accept.ServiceProvider.GetRequiredService<IUserInvitationService>();
        await service.AcceptAsync(token, null, null, userId, CancellationToken.None);

        var auth = accept.ServiceProvider.GetRequiredService<AuthDbContext>();
        Assert.True(await auth.CompanyUsers.AnyAsync(
            x => x.UserId == userId && x.Role == "Admin" && x.IsActive));
    }

    [Theory]
    [InlineData("Reader")]
    [InlineData("Editor")]
    [InlineData("Admin")]
    public async Task Valid_roles_are_accepted(string role)
    {
        var (_, _, _) = await InviteAsync(NewEmail(), role);

        using var scope = _fixture.CreateScope();
        var auth = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        Assert.Contains(await auth.Set<TenantInvitation>().ToListAsync(),
            x => x.Role == role);
    }

    [Theory]
    [InlineData("SuperAdmin")]
    [InlineData("reader")]
    [InlineData("")]
    public async Task Invalid_roles_are_rejected(string role)
    {
        using var scope = _fixture.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IUserInvitationService>();

        await Assert.ThrowsAsync<ArgumentException>(() => service.InviteAsync(
            InvitationFixture.TenantId, _fixture.Admin.Id,
            new InviteTenantUserRequest(NewEmail(), _fixture.Company.Id, role),
            CancellationToken.None));
    }

    [Fact]
    public async Task A_non_admin_cannot_invite()
    {
        using var scope = _fixture.CreateScope();
        var auth = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        var users = scope.ServiceProvider.GetRequiredService<
            Microsoft.AspNetCore.Identity.UserManager<ApplicationUser>>();

        var outsider = InvitationFixture.NewUser(NewEmail());
        await users.CreateAsync(outsider, "Aa1!contrasenaLarga");
        auth.TenantUsers.Add(new TenantUser
        { TenantId = InvitationFixture.TenantId, UserId = outsider.Id });
        auth.SaveChanges();

        var service = scope.ServiceProvider.GetRequiredService<IUserInvitationService>();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.InviteAsync(
            InvitationFixture.TenantId, outsider.Id,
            new InviteTenantUserRequest(NewEmail(), _fixture.Company.Id, "Reader"),
            CancellationToken.None));
    }
}