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
    public Task<ApiResult<CustomerDto>> CreateAsync(Guid companyId, SaveCustomerDto request, CancellationToken ct = default) =>
        _api.PostAsync<CustomerDto>(ApiPaths.Customers(companyId), request, ct);

    public Task<ApiResult<CustomerDto>> UpdateAsync(Guid companyId, Guid customerId, SaveCustomerDto request, CancellationToken ct = default) =>
        _api.PutAsync<CustomerDto>($"{ApiPaths.Customers(companyId)}/{customerId:D}", request, ct);

    public Task<ApiResult<object>> DeleteAsync(Guid companyId, Guid customerId, CancellationToken ct = default) =>
        _api.DeleteAsync<object>($"{ApiPaths.Customers(companyId)}/{customerId:D}", ct);
}


public sealed record CustomerDto(Guid Id, Guid CompanyId, string TaxId, string LegalName,
    string Address, string PostalCode, string City, string CountryCode, string? Email, bool IsBusiness);

public sealed record SaveCustomerDto(string TaxId, string LegalName, string Address, string PostalCode,
    string City, string CountryCode, string? Email, bool IsBusiness);
