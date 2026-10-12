using System.Text.Json;
using Facturacion.Domain.Entities;
using Facturacion.Application.Provisioning;
using Facturacion.Infrastructure.Identity;
using Facturacion.Infrastructure.Multitenancy;
using Facturacion.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
namespace Facturacion.Infrastructure.Billing;

/// <summary>
/// Resultado del procesamiento. Ignored/Handled cortan la cadena (2xx, Stripe no
/// reintenta); RetryLater devuelve 503 para que Stripe reintente.
/// </summary>
public enum StripeEventOutcome { Handled, Ignored, RetryLater }

public sealed class StripeEventProcessor
{
    private readonly SelfServiceDbContext _self;
    private readonly IProvisioningService _provisioning;
    private readonly BillingDbContext _billing;
    private readonly AuthDbContext _auth;
    private readonly TenantContext _tenant;
    private readonly ILogger<StripeEventProcessor> _logger;
    public StripeEventProcessor(SelfServiceDbContext self, BillingDbContext billing,
        AuthDbContext auth, TenantContext tenant, IProvisioningService provisioning,
        ILogger<StripeEventProcessor> logger)
    { _self=self; _billing=billing; _auth=auth; _tenant=tenant; _provisioning=provisioning; _logger=logger; }

    public async Task<StripeEventOutcome> HandleAsync(JsonElement evt, CancellationToken ct)
    {
        if (!evt.TryGetProperty("id", out var idElement) || string.IsNullOrWhiteSpace(idElement.GetString()))
        {
            _logger.LogWarning("Evento Stripe sin id; se ignora.");
            return StripeEventOutcome.Ignored;
        }
        var eventId=idElement.GetString()!;

        if (await _self.ProcessedPaymentEvents.AnyAsync(x=>x.Id==eventId,ct))
            return StripeEventOutcome.Ignored;

        var type=evt.TryGetProperty("type",out var t) ? t.GetString() : null;
        var obj=evt.TryGetProperty("data",out var d) && d.TryGetProperty("object",out var o) ? o : default;

        if (type=="checkout.session.completed" && obj.ValueKind!=JsonValueKind.Undefined)
        {
            var mode=obj.TryGetProperty("mode",out var m) ? m.GetString() : null;
            var paid=obj.TryGetProperty("payment_status",out var p) ? p.GetString() : null;
            if (mode=="subscription" && paid=="paid")
            {
                // Fallos permanentes -> 2xx (se registran pero no se reintenta en
                // bucle durante dias); fallos transitorios -> 503.
                if (!await TryActivateAsync(obj,ct))
                    return StripeEventOutcome.RetryLater;
            }
        }
        else if (type is "customer.subscription.updated" or "customer.subscription.deleted"
                 && obj.ValueKind!=JsonValueKind.Undefined)
        {
            var subId=obj.TryGetProperty("id",out var s) ? s.GetString() : null;
            var subscription=await _self.SubscriptionStates
                .FirstOrDefaultAsync(x=>x.StripeSubscriptionId==subId,ct);
            if (subscription is not null)
            {
                subscription.Status=obj.TryGetProperty("status",out var st) ? st.GetString() ?? "unknown" : "unknown";
                await _self.SaveChangesAsync(ct);
            }
            // Un evento anterior a checkout puede llegar primero; reconciliar con Stripe API.
        }

        // Marcar el evento procesado solo al finalizar correctamente la operacion.
        _self.ProcessedPaymentEvents.Add(new ProcessedPaymentEvent { Id=eventId });
        await _self.SaveChangesAsync(ct);
        return StripeEventOutcome.Handled;
    }

    /// <returns>false solo ante fallos transitorios que deben reintentarse.</returns>
    private async Task<bool> TryActivateAsync(JsonElement session, CancellationToken ct)
    {
        var sessionId=session.TryGetProperty("id",out var s) ? s.GetString() : null;
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            _logger.LogWarning("checkout.session.completed sin id de sesion; se ignora.");
            return true;
        }

        var intent=await _self.RegistrationIntents.SingleOrDefaultAsync(x=>x.CheckoutSessionId==sessionId,ct);
        if (intent is null)
        {
            _logger.LogWarning(
                "Sesion Stripe {SessionId} sin registro local; se ignora de forma permanente.", sessionId);
            return true;
        }
        if (intent.Status=="Active" && await _self.SubscriptionStates.AnyAsync(
            x=>x.TenantId==intent.TenantId,ct)) return true;

        var user=await _auth.Users.SingleOrDefaultAsync(x=>x.Id==intent.UserId && x.EmailConfirmed,ct);
        if (user is null)
        {
            _logger.LogWarning(
                "Usuario {UserId} sin correo confirmado; no se aprovisiona.", intent.UserId);
            return true;
        }

        var stripeSub=session.TryGetProperty("subscription",out var ss) ? ss.GetString() : null;
        if (string.IsNullOrWhiteSpace(stripeSub)) return true;
        var stripeCustomer=session.TryGetProperty("customer",out var sc) ? sc.GetString() : null;
        if (string.IsNullOrWhiteSpace(stripeCustomer)) return true;

        try
        {
            var result = await _provisioning.ProvisionAsync(new ProvisioningRequest(
                intent.Id, intent.UserId, intent.TenantName, intent.LegalName,
                intent.Nif, intent.Address, intent.PostalCode, intent.City, intent.Province), ct);
            _self.SubscriptionStates.Add(new SubscriptionState { TenantId=result.TenantId,
                StripeCustomerId=stripeCustomer, StripeSubscriptionId=stripeSub,
                PlanCode=intent.PlanCode, Status="active" });
            intent.Status="Active";
            await _self.SaveChangesAsync(ct);
            return true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex,
                "Fallo al aprovisionar el tenant del registro {RegistrationId}.", intent.Id);
            return false;
        }
    }
}