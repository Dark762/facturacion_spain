namespace Facturacion.Client;

public sealed class ProductsApiClient
{
    private readonly ApiClient _api;
    public ProductsApiClient(ApiClient api) => _api = api;

    public Task<ApiResult<IReadOnlyList<ProductDto>>> ListAsync(Guid companyId, CancellationToken ct = default) =>
        _api.GetAsync<IReadOnlyList<ProductDto>>(ApiPaths.Products(companyId), ct);

    public Task<ApiResult<ProductDto>> GetAsync(Guid companyId, Guid productId, CancellationToken ct = default) =>
        _api.GetAsync<ProductDto>($"{ApiPaths.Products(companyId)}/{productId:D}", ct);

    public Task<ApiResult<ProductDto>> CreateAsync(Guid companyId, SaveProductDto request, CancellationToken ct = default) =>
        _api.PostAsync<ProductDto>(ApiPaths.Products(companyId), request, ct);

    public Task<ApiResult<ProductDto>> UpdateAsync(Guid companyId, Guid productId, SaveProductDto request, CancellationToken ct = default) =>
        _api.PutAsync<ProductDto>($"{ApiPaths.Products(companyId)}/{productId:D}", request, ct);

    public Task<ApiResult<object>> DeleteAsync(Guid companyId, Guid productId, CancellationToken ct = default) =>
        _api.DeleteAsync<object>($"{ApiPaths.Products(companyId)}/{productId:D}", ct);
}

public sealed record ProductDto(Guid Id, Guid CompanyId, string Code, string Name,
    string Description, decimal UnitPrice, decimal VatRate, bool IsActive);
public sealed record SaveProductDto(string Code, string Name, string Description,
    decimal UnitPrice, decimal VatRate, bool IsActive);
