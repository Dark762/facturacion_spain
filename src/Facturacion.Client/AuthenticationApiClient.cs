namespace Facturacion.Client;

/// <summary>Acceso a la API de autenticación desde cualquier host compatible con HttpClient.</summary>
public sealed class AuthenticationApiClient
{
    private readonly ApiClient _api;
    public AuthenticationApiClient(ApiClient api) => _api = api;

    public Task<ApiResult<LoginDto>> LoginAsync(string email, string password, Guid tenantId, CancellationToken ct = default) =>
        _api.PostAsync<LoginDto>(ApiPaths.Login, new { email, password, tenantId }, ct);
}

public sealed record LoginDto(string AccessToken, DateTime ExpiresUtc);
