using System.Net;
using System.Text;
using Facturacion.Client;

namespace Facturacion.Integration.Test;

public sealed class CustomersApiClientTests
{
    private sealed class Handler(Func<HttpRequestMessage, HttpResponseMessage> response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(response(request));
    }

    [Fact]
    public async Task List_uses_company_scoped_endpoint_and_deserializes_customers()
    {
        var companyId = Guid.NewGuid();
        using var http = new HttpClient(new Handler(request =>
        {
            Assert.Equal($"/api/companies/{companyId:D}/customers", request.RequestUri!.AbsolutePath);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent($@"[{{""id"":""{Guid.NewGuid():D}"",""companyId"":""{companyId:D}"",""taxId"":""B12345678"",""legalName"":""Ejemplo SL"",""address"":""Calle Mayor 1"",""postalCode"":""28001"",""city"":""Madrid"",""countryCode"":""ES"",""email"":null,""isBusiness"":true}}]", Encoding.UTF8, "application/json")
            };
        })) { BaseAddress = new Uri("https://api.example.test/") };
        var result = await new CustomersApiClient(new ApiClient(http)).ListAsync(companyId);
        Assert.True(result.IsSuccess);
        Assert.Equal(companyId, Assert.Single(result.Value!).CompanyId);
    }

    [Fact]
    public async Task Search_encodes_text_and_sends_page_parameters()
    {
        var companyId = Guid.NewGuid();
        using var http = new HttpClient(new Handler(request =>
        {
            Assert.Equal($"/api/companies/{companyId:D}/customers", request.RequestUri!.AbsolutePath);
            Assert.Contains("page=2", request.RequestUri.Query);
            Assert.Contains("pageSize=20", request.RequestUri.Query);
            Assert.Contains("search=ACME%20%26%20Co", request.RequestUri.Query);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("[]", Encoding.UTF8, "application/json")
            };
        })) { BaseAddress = new Uri("https://api.example.test/") };

        var result = await new CustomersApiClient(new ApiClient(http))
            .SearchAsync(companyId, " ACME & Co ", 2, 20);
        Assert.True(result.IsSuccess);
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public async Task Search_rejects_invalid_page_options(int page, int pageSize)
    {
        using var http = new HttpClient(new Handler(_ =>
            throw new InvalidOperationException("No debería enviar peticiones")))
        { BaseAddress = new Uri("https://api.example.test/") };
        var api = new CustomersApiClient(new ApiClient(http));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            api.SearchAsync(Guid.NewGuid(), null, page, pageSize));
    }

    [Fact]
    public async Task Search_rejects_overlong_text_without_http_request()
    {
        using var http = new HttpClient(new Handler(_ =>
            throw new InvalidOperationException("No debería enviar peticiones")))
        { BaseAddress = new Uri("https://api.example.test/") };
        var api = new CustomersApiClient(new ApiClient(http));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            api.SearchAsync(Guid.NewGuid(), new string('X', 121)));
    }

    [Fact]
    public async Task Forbidden_company_returns_error_without_customer_data()
    {
        using var http = new HttpClient(new Handler(_ => new HttpResponseMessage(HttpStatusCode.Forbidden)))
        { BaseAddress = new Uri("https://api.example.test/") };
        var result = await new CustomersApiClient(new ApiClient(http)).ListAsync(Guid.NewGuid());
        Assert.False(result.IsSuccess);
        Assert.Null(result.Value);
        Assert.Equal(HttpStatusCode.Forbidden, result.StatusCode);
    }
}
