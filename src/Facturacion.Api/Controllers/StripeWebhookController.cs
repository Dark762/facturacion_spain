using System.Text;
using System.Text.Json;
using Facturacion.Infrastructure.Billing;
using Facturacion.Payments.Stripe;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
namespace Facturacion.Api.Controllers;
[ApiController, Route("api/webhooks/stripe")]
public sealed class StripeWebhookController : ControllerBase
{
    private readonly StripeEventProcessor _processor;
    private readonly StripeOptions _options;
    public StripeWebhookController(StripeEventProcessor processor,IOptions<StripeOptions> options)
    { _processor=processor; _options=options.Value; }
    [AllowAnonymous, HttpPost]
    [RequestSizeLimit(1048576)]
    public async Task<IActionResult> Receive(CancellationToken ct)
    {
        // La firma se calcula sobre los bytes exactos del cuerpo; leerlo como
        // texto y recodificar puede invalidar webhooks legitimos.
        using var buffer=new MemoryStream();
        await Request.Body.CopyToAsync(buffer,ct);
        var payload=buffer.ToArray();

        if (!StripeWebhookVerifier.Verify(payload,Request.Headers["Stripe-Signature"].ToString(),_options.WebhookSecret))
            return BadRequest();

        JsonDocument doc;
        try { doc=JsonDocument.Parse(Encoding.UTF8.GetString(payload)); }
        catch (JsonException) { return BadRequest(); }

        using (doc)
        {
            var outcome = await _processor.HandleAsync(doc.RootElement,ct);
            // Un evento que no se puede actuar nunca se reintentara en Stripe si
            // devolvemos 2xx; los errores transitorios si deben reintentarse.
            return outcome == StripeEventOutcome.RetryLater
                ? StatusCode(StatusCodes.Status503ServiceUnavailable)
                : Ok();
        }
    }
}