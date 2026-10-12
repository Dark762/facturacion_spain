using Facturacion.Application.Billing;
using Facturacion.Infrastructure.Billing;
using Facturacion.Integration.Test.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Facturacion.Integration.Test;

/// <summary>
/// Regresion del bypass de verificacion de correo: cuando el correo ya estaba
/// confirmado, un token invalido seguia emitiendo un CheckoutToken valido.
/// Basta conocer el RegistrationId (un Guid que el alta devuelve en claro).
/// </summary>
public sealed class SaasRegistrationTests : IClassFixture<RegistrationFixture>
{
    private const string Password = "Aa1!contrasenaSegura";

    private readonly RegistrationFixture _fixture;

    public SaasRegistrationTests(RegistrationFixture fixture) => _fixture = fixture;

    private static RegisterSaasRequest Request(string email) => new(
        email, Password, "Dueño",
        "Grupo Demo", "B12345678", "Demo SL",
        "Calle Mayor 1", "28001", "Madrid", "Madrid");

    private async Task<Guid> RegisterAsync(string email)
    {
        using var scope = _fixture.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<SaasRegistrationService>();
        var response = await service.RegisterAsync(Request(email), CancellationToken.None);
        return response.RegistrationId;
    }

    [Fact]
    public async Task Register_creates_an_unconfirmed_user_and_sends_the_confirmation()
    {
        var email = $"alta-{Guid.NewGuid():N}@example.test";
        var id = await RegisterAsync(email);

        var sent = Assert.Single(_fixture.Emails.Sent, x => x.RegistrationId == id);
        Assert.Equal(email, sent.Email);
        Assert.False(string.IsNullOrWhiteSpace(sent.Token));
    }

    [Fact]
    public async Task Verify_with_the_real_token_issues_a_checkout_token()
    {
        var email = $"verify-{Guid.NewGuid():N}@example.test";
        var id = await RegisterAsync(email);
        var token = _fixture.Emails.Sent.Single(x => x.RegistrationId == id).Token;

        using var scope = _fixture.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<SaasRegistrationService>();

        var checkoutToken = await service.VerifyEmailAsync(id, token, CancellationToken.None);

        Assert.Equal(64, checkoutToken.Length);
        var intent = _fixture.GetDb<SelfServiceDbContext>(scope)
            .RegistrationIntents.Single(x => x.Id == id);
        Assert.Equal("EmailVerified", intent.Status);
        Assert.NotNull(intent.CheckoutTokenHash);
    }

    [Fact]
    public async Task Invalid_token_is_rejected_on_first_verification()
    {
        var email = $"bad-{Guid.NewGuid():N}@example.test";
        var id = await RegisterAsync(email);

        using var scope = _fixture.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<SaasRegistrationService>();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.VerifyEmailAsync(id, "token-invalido", CancellationToken.None));
    }

    [Fact]
    public async Task Invalid_token_does_not_reissue_a_checkout_token_once_already_confirmed()
    {
        var email = $"replay-{Guid.NewGuid():N}@example.test";
        var id = await RegisterAsync(email);
        var token = _fixture.Emails.Sent.Single(x => x.RegistrationId == id).Token;

        using (var initial = _fixture.CreateScope())
        {
            var initialService = initial.ServiceProvider.GetRequiredService<SaasRegistrationService>();
            await initialService.VerifyEmailAsync(id, token, CancellationToken.None);
        }

        // REGRESION: antes, este caso devolvia 200 y un CheckoutToken nuevo
        // porque la condicion era "!Succeeded && !EmailConfirmed".
        using var scope = _fixture.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<SaasRegistrationService>();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.VerifyEmailAsync(id, "cualquier-token-cualquiera", CancellationToken.None));

        var intent = _fixture.GetDb<SelfServiceDbContext>(scope)
            .RegistrationIntents.Single(x => x.Id == id);
        Assert.Equal("EmailVerified", intent.Status);
    }

    [Fact]
    public async Task Duplicate_email_is_rejected_without_leaking_password_policy_details()
    {
        var email = $"dup-{Guid.NewGuid():N}@example.test";
        await RegisterAsync(email);

        using var scope = _fixture.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<SaasRegistrationService>();

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.RegisterAsync(Request(email), CancellationToken.None));

        Assert.DoesNotContain("Password", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("contraseña", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Unknown_registration_id_is_rejected()
    {
        using var scope = _fixture.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<SaasRegistrationService>();

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.VerifyEmailAsync(Guid.NewGuid(), "cualquier-token", CancellationToken.None));
    }

    [Fact]
    public async Task Missing_mandatory_fields_are_rejected()
    {
        using var scope = _fixture.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<SaasRegistrationService>();

        var incomplete = Request($"incompleto-{Guid.NewGuid():N}@example.test") with
        { Nif = "   ", City = "" };

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.RegisterAsync(incomplete, CancellationToken.None));
    }
}