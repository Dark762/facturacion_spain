namespace Facturacion.Client;

/// <summary>Consulta de clientes por empresa usando los endpoints autorizados de la API.</summary>
public sealed class CustomersApiClient
{
    private readonly ApiClient _api;
    public CustomersApiClient(ApiClient api) => _api = api;

    public Task<ApiResult<IReadOnlyList<CustomerDto>>> ListAsync(Guid companyId, CancellationToken ct = default) =>
        _api.GetAsync<IReadOnlyList<CustomerDto>>(ApiPaths.Customers(companyId), ct);

    public Task<ApiResult<CustomerDto>> GetAsync(Guid companyId, Guid customerId, CancellationToken ct = default) =>
        _api.GetAsync<CustomerDto>($"{ApiPaths.Customers(companyId)}/{customerId:D}", ct);
}

public sealed record CustomerDto(Guid Id, Guid CompanyId, string TaxId, string LegalName,
    string Address, string PostalCode, string City, string CountryCode, string? Email, bool IsBusiness);
