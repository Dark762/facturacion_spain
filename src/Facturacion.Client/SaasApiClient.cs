namespace Facturacion.Client;

/// <summary>Cliente HTTP compartido para el alta SaaS; DTOs propios, sin referencias a Infrastructure.</summary>
public sealed class SaasApiClient
{
    private readonly ApiClient _api;
    public SaasApiClient(ApiClient api) => _api = api;

    public Task<ApiResult<IReadOnlyList<PlanDto>>> GetPlansAsync(CancellationToken ct = default) =>
        _api.GetAsync<IReadOnlyList<PlanDto>>(ApiPaths.Plans, ct);

    public Task<ApiResult<RegistrationDto>> RegisterAsync(RegisterDto request, CancellationToken ct = default) =>
        _api.PostAsync<RegistrationDto>(ApiPaths.Register, request, ct);

    public Task<ApiResult<VerificationDto>> VerifyEmailAsync(Guid registrationId, string token, CancellationToken ct = default) =>
        _api.PostAsync<VerificationDto>(ApiPaths.VerifyEmail, new { registrationId, token }, ct);

    public Task<ApiResult<object>> CompleteCompanyAsync(Guid registrationId, string checkoutToken, CompanyDto company, CancellationToken ct = default) =>
        _api.PostAsync<object>(ApiPaths.Company, new { registrationId, checkoutToken, company }, ct);

    public Task<ApiResult<CheckoutDto>> CheckoutAsync(Guid registrationId, string checkoutToken, string planCode, CancellationToken ct = default) =>
        _api.PostAsync<CheckoutDto>(ApiPaths.Checkout, new { registrationId, checkoutToken, planCode }, ct);
}

public sealed record PlanDto(string Code, string Name, decimal MonthlyPrice, string Currency, int MaxCompanies, int MaxUsers, int MaxInvoicesPerMonth, string Description);
public sealed record RegisterDto(string Email, string Password, string DisplayName);
public sealed record RegistrationDto(Guid RegistrationId, string Message);
public sealed record VerificationDto(string CheckoutToken);
public sealed record CompanyDto(string LegalName, string Nif, string Address, string PostalCode, string City, string Province, string HolderType = "Individual", string TradeName = "");
public sealed record CheckoutDto(string Url);
