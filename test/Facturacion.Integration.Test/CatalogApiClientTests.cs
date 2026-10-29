using System.Net;
using System.Text;
using Facturacion.Client;

namespace Facturacion.Integration.Test;

public sealed class CatalogApiClientTests
{
    private sealed class Handler(Func<HttpRequestMessage, HttpResponseMessage> callback) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(callback(request));
    }

    [Fact]
    public async Task Products_list_uses_company_scoped_route()
    {
        var companyId = Guid.NewGuid();
        using var http = new HttpClient(new Handler(request =>
        {
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal($"/api/companies/{companyId:D}/products", request.RequestUri!.AbsolutePath);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("[]", Encoding.UTF8, "application/json")
            };
        })) { BaseAddress = new Uri("https://api.example.test/") };
        var result = await new ProductsApiClient(new ApiClient(http)).ListAsync(companyId);
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!);
    }

    [Fact]
    public async Task Products_create_sends_json_and_propagates_conflict()
    {
        var companyId = Guid.NewGuid();
        using var http = new HttpClient(new Handler(request =>
        {
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal($"/api/companies/{companyId:D}/products", request.RequestUri!.AbsolutePath);
            Assert.Equal("application/json", request.Content!.Headers.ContentType!.MediaType);
            return new HttpResponseMessage(HttpStatusCode.Conflict);
        })) { BaseAddress = new Uri("https://api.example.test/") };
        var result = await new ProductsApiClient(new ApiClient(http))
            .CreateAsync(companyId, new SaveProductDto("A1", "Servicio", "", 100, 21, true));
        Assert.False(result.IsSuccess);
        Assert.Equal(HttpStatusCode.Conflict, result.StatusCode);
    }

    [Fact]
    public async Task Customers_update_uses_put_and_company_route()
    {
        var companyId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        using var http = new HttpClient(new Handler(request =>
        {
            Assert.Equal(HttpMethod.Put, request.Method);
            Assert.Equal($"/api/companies/{companyId:D}/customers/{customerId:D}", request.RequestUri!.AbsolutePath);
            return new HttpResponseMessage(HttpStatusCode.Forbidden);
        })) { BaseAddress = new Uri("https://api.example.test/") };
        var result = await new CustomersApiClient(new ApiClient(http))
            .UpdateAsync(companyId, customerId, new SaveCustomerDto("B12345678", "Empresa", "", "", "", "ES", null, true));
        Assert.False(result.IsSuccess);
        Assert.Equal(HttpStatusCode.Forbidden, result.StatusCode);
    }

    [Fact]
    public async Task Products_delete_uses_delete_and_returns_no_content()
    {
        var companyId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        using var http = new HttpClient(new Handler(request =>
        {
            Assert.Equal(HttpMethod.Delete, request.Method);
            Assert.Equal($"/api/companies/{companyId:D}/products/{productId:D}", request.RequestUri!.AbsolutePath);
            return new HttpResponseMessage(HttpStatusCode.NoContent);
        })) { BaseAddress = new Uri("https://api.example.test/") };
        var result = await new ProductsApiClient(new ApiClient(http)).DeleteAsync(companyId, productId);
        Assert.True(result.IsSuccess);
    }
}
