using Facturacion.Application.Billing;
using Facturacion.Configuration.Options;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text.Json;


namespace Facturacion.Payments.Stripe;
public sealed class StripeCheckoutGateway : IPaymentGateway
{
    private readonly HttpClient _http;
    private readonly StripeOptions _options;
    public StripeCheckoutGateway(HttpClient http, IOptions<StripeOptions> options)
    { _http = http; _options = options.Value; }
    public async Task<(string SessionId, string Url)> CreateSubscriptionCheckoutAsync(
        string priceId, string email, string registrationId, string successUrl,
        string cancelUrl, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.stripe.com/v1/checkout/sessions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.SecretKey);
        request.Content = new FormUrlEncodedContent(new Dictionary<string,string>
        {
            ["mode"] = "subscription", ["line_items[0][price]"] = priceId,
            ["line_items[0][quantity]"] = "1", ["customer_email"] = email,
            ["client_reference_id"] = registrationId,
            ["metadata[registration_id]"] = registrationId,
            ["subscription_data[metadata][registration_id]"] = registrationId,
            ["success_url"] = successUrl, ["cancel_url"] = cancelUrl
        });
        using var response = await _http.SendAsync(request, ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Stripe Checkout devolvió {(int)response.StatusCode}; revisa el registro seguro del servidor.");
        using var json = JsonDocument.Parse(body);
        return (json.RootElement.GetProperty("id").GetString()!, json.RootElement.GetProperty("url").GetString()!);
    }
}
