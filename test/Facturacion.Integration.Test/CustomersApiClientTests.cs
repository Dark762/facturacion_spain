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
