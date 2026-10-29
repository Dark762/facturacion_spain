using System.Net;
using System.Text;
using Facturacion.Client;

namespace Facturacion.Integration.Test;

public sealed class ApiHttpClientTests
{
    private sealed class StubHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> callback) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            callback(request, cancellationToken);
    }

    private static ApiClient Client(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> callback) =>
        new(new HttpClient(new StubHandler(callback)) { BaseAddress = new Uri("https://api.example.test/") });

    [Fact]
    public async Task Get_deserializes_successful_json()
    {
        var api = Client((request, _) =>
        {
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal("/api/saas/plans", request.RequestUri!.AbsolutePath);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("[{\"code\":\"basic\",\"name\":\"Básico\",\"monthlyPrice\":10,\"currency\":\"EUR\",\"maxCompanies\":1,\"maxUsers\":1,\"maxInvoicesPerMonth\":50,\"description\":\"Inicio\"}]", Encoding.UTF8, "application/json")
            });
        });
        var result = await new SaasApiClient(api).GetPlansAsync();
        Assert.True(result.IsSuccess);
        Assert.Equal("basic", Assert.Single(result.Value!).Code);
    }

    [Fact]
    public async Task Validation_problem_preserves_field_errors()
    {
        var api = Client((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("{\"title\":\"Validation failed\",\"errors\":{\"Password\":[\"Muy corta\"]}}", Encoding.UTF8, "application/problem+json")
        }));
        var result = await api.PostAsync<object>(ApiPaths.Register, new { email = "a@example.com" });
        Assert.False(result.IsSuccess);
        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Equal("Muy corta", Assert.Single(result.Error!.ValidationErrors!["Password"]));
    }

    [Fact]
    public async Task No_content_is_successful()
    {
        var api = Client((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent)));
        var result = await api.PostAsync<object>(ApiPaths.Company, new { });
        Assert.True(result.IsSuccess);
        Assert.Null(result.Value);
    }

    [Fact]
    public async Task Non_json_errors_have_safe_fallback()
    {
        var api = Client((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadGateway)
        {
            Content = new StringContent("<html>Gateway failure</html>", Encoding.UTF8, "text/html")
        }));
        var result = await api.GetAsync<object>(ApiPaths.Plans);
        Assert.False(result.IsSuccess);
        Assert.Contains("502", result.Error!.Message);
    }

    [Fact]
    public async Task Absolute_urls_are_rejected()
    {
        var api = Client((_, _) => throw new InvalidOperationException("No debe enviar solicitudes"));
        await Assert.ThrowsAsync<ArgumentException>(() => api.GetAsync<object>("https://evil.example.test/"));
    }

    [Fact]
    public async Task Cancellation_is_forwarded_to_handler()
    {
        using var source = new CancellationTokenSource();
        var api = Client((_, ct) =>
        {
            Assert.Equal(source.Token, ct);
            throw new OperationCanceledException(ct);
        });
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => api.GetAsync<object>(ApiPaths.Plans, source.Token));
    }

    [Fact]
    public async Task Invoice_client_uses_existing_invoice_endpoint()
    {
        var id = Guid.NewGuid();
        var api = Client((request, _) =>
        {
            Assert.Equal($"/api/invoices/{id:D}", request.RequestUri!.AbsolutePath);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent($@"{{""id"":""{id:D}"",""companyId"":""{Guid.NewGuid():D}"",""customerId"":""{Guid.NewGuid():D}"",""series"":null,""number"":null,""taxableBase"":100,""taxAmount"":21,""total"":121,""status"":""Draft""}}", Encoding.UTF8, "application/json")
            });
        });
        var result = await new InvoicesApiClient(api).GetAsync(id);
        Assert.True(result.IsSuccess);
        Assert.Equal(id, result.Value!.Id);
    }
}
