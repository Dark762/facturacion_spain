using Facturacion.Application.Billing;
using Facturacion.Application.Common.Results;
using Facturacion.Application.Services;
using Xunit;

namespace Facturacion.UnitTests;

/// <summary>
/// Regresión: los BusinessException del servicio de registro llegaban a la UI y
/// se sustituían por un mensaje genérico. La capa de aplicación debe convertirlos
/// en errores del Result conservando código y mensaje.
/// </summary>
public sealed class SaasRegistrationApplicationServiceTests
{
    private const string Password = "Aa1!contrasenaSegura";

    private static RegisterSaasRequest Request() =>
        new("alta@example.test", Password, "Dueño");

    [Fact]
    public async Task Register_surfaces_the_business_message()
    {
        const string message =
            "Este correo ya está registrado. Si comenzaste el registro y no lo completaste, utiliza «Continuar registro pendiente».";
        var service = Service(new BusinessException("REGISTRATION_EMAIL_EXISTS", message, 409));

        var result = await service.RegisterAsync(Request(), CancellationToken.None);

        Assert.True(result.IsFailure);
        var error = Assert.Single(result.Errors);
        Assert.Equal("REGISTRATION_EMAIL_EXISTS", error.Code);
        Assert.Equal(message, error.Message);
        Assert.Equal("Email", error.Field);
        Assert.Equal(ErrorType.Conflict, error.Type);
    }

    [Fact]
    public async Task Register_translates_identity_password_errors_on_the_password_field()
    {
        var service = Service(new BusinessException(
            "REGISTRATION_INVALID_PASSWORD",
            "Passwords must be at least 12 characters.; Passwords must have at least one non alphanumeric character.",
            400));

        var result = await service.RegisterAsync(Request(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(2, result.Errors.Count);
        Assert.All(result.Errors, error =>
        {
            Assert.Equal("PASSWORD_INVALID", error.Code);
            Assert.Equal("Password", error.Field);
        });
        Assert.Contains(result.Errors, error => error.Message.Contains("12 caracteres"));
        Assert.Contains(result.Errors, error => error.Message.Contains("carácter especial"));
    }

    [Fact]
    public async Task Register_preserves_required_fields_message()
    {
        var service = Service(new BusinessException(
            "REGISTRATION_REQUIRED_FIELDS", "Faltan datos obligatorios de registro.", 400));

        var result = await service.RegisterAsync(Request(), CancellationToken.None);

        var error = Assert.Single(result.Errors);
        Assert.Equal("REGISTRATION_REQUIRED_FIELDS", error.Code);
        Assert.Equal("Faltan datos obligatorios de registro.", error.Message);
        Assert.Equal("request", error.Field);
        Assert.Equal(ErrorType.Validation, error.Type);
    }

    [Fact]
    public async Task VerifyEmail_surfaces_not_found_message()
    {
        var service = Service(new BusinessException(
            "REGISTRATION_NOT_FOUND", "Registro inexistente.", 404));

        var result = await service.VerifyEmailAsync(Guid.NewGuid(), "123456", CancellationToken.None);

        var error = Assert.Single(result.Errors);
        Assert.Equal("REGISTRATION_NOT_FOUND", error.Code);
        Assert.Equal("Registro inexistente.", error.Message);
        Assert.Equal(ErrorType.NotFound, error.Type);
    }

    [Fact]
    public async Task CompleteCompany_surfaces_not_verified_message()
    {
        var service = Service(new BusinessException(
            "REGISTRATION_NOT_VERIFIED", "Registro no verificado o sesión caducada.", 403));

        var result = await service.CompleteCompanyAsync(
            Guid.NewGuid(), "token",
            new CompleteCompanyRequest("Demo SL", "B12345678", "Calle Mayor 1",
                "28001", "Madrid", "Madrid"),
            CancellationToken.None);

        var error = Assert.Single(result.Errors);
        Assert.Equal("REGISTRATION_NOT_VERIFIED", error.Code);
        Assert.Equal("Registro no verificado o sesión caducada.", error.Message);
        Assert.Equal(ErrorType.Forbidden, error.Type);
    }

    [Fact]
    public async Task Checkout_surfaces_conflict_message()
    {
        var service = Service(new BusinessException(
            "REGISTRATION_CHECKOUT_EXISTS",
            "Ya existe una sesión de pago. Consulta su estado antes de crear otra.", 409));

        var result = await service.CheckoutAsync(Guid.NewGuid(), "token", "PRO", CancellationToken.None);

        var error = Assert.Single(result.Errors);
        Assert.Equal("REGISTRATION_CHECKOUT_EXISTS", error.Code);
        Assert.Equal("Ya existe una sesión de pago. Consulta su estado antes de crear otra.", error.Message);
        Assert.Equal(ErrorType.Conflict, error.Type);
    }

    [Fact]
    public async Task Register_returns_the_service_response_on_success()
    {
        var id = Guid.NewGuid();
        var service = new SaasRegistrationApplicationService(new StubRegistrationService
        {
            Registration = new RegistrationResponse(id, "Comprueba tu correo.")
        });

        var result = await service.RegisterAsync(Request(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(id, result.Value.RegistrationId);
    }

    private static SaasRegistrationApplicationService Service(Exception failure) =>
        new(new StubRegistrationService { Throws = failure });

    private sealed class StubRegistrationService : ISaasRegistrationService
    {
        public Exception? Throws { get; init; }
        public RegistrationResponse Registration { get; init; } = new(Guid.NewGuid(), "ok");
        public RegistrationProgress Progress { get; init; } = new(
            "checkout", false, "Dueño", "Individual", "", "", "", "", "", "", "");
        public string CheckoutToken { get; init; } = "checkout-token";
        public CheckoutResponse Checkout { get; init; } = new("https://checkout.stripe.com/session");

        private Task<T> Run<T>(T value) =>
            Throws is null ? Task.FromResult(value) : Task.FromException<T>(Throws);

        public Task<RegistrationResponse> RegisterAsync(RegisterSaasRequest request, CancellationToken ct) =>
            Run(Registration);

        public Task<RegistrationResponse> ResumeAsync(ResumeRegistrationRequest request, CancellationToken ct) =>
            Run(Registration);

        public Task<RegistrationProgress> VerifyAndResumeAsync(
            Guid registrationId, string token, string newPassword, CancellationToken ct) =>
            Run(Progress);

        public Task<string> VerifyEmailAsync(Guid registrationId, string token, CancellationToken ct) =>
            Run(CheckoutToken);

        public Task CompleteCompanyAsync(
            Guid registrationId, string checkoutToken, CompleteCompanyRequest request, CancellationToken ct) =>
            Throws is null ? Task.CompletedTask : Task.FromException(Throws);

        public Task<CheckoutResponse> CheckoutAsync(
            Guid registrationId, string checkoutToken, string planCode, CancellationToken ct) =>
            Run(Checkout);
    }
}
