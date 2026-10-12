using System.Security.Cryptography;
using System.Text;
using Facturacion.Payments.Stripe;

namespace Facturacion.Integration.Test;

/// <summary>
/// La verificacion de firma es la unica barrera del webhook. Se prueba contra
/// una implementacion de referencia del algoritmo de Stripe.
/// </summary>
public sealed class StripeWebhookVerifierTests
{
    private const string Secret = "whsec_test_secret_value_0123456789";

    private static string Sign(string secret, long timestamp, byte[] payload)
    {
        var signed = Encoding.UTF8.GetBytes(timestamp + ".").Concat(payload).ToArray();
        return Convert.ToHexString(HMACSHA256.HashData(
            Encoding.UTF8.GetBytes(secret), signed)).ToLowerInvariant();
    }

    private static string Header(long timestamp, params string[] v1) =>
        $"t={timestamp},{string.Join(",", v1.Select(v => "v1=" + v))}";

    private static byte[] Payload(string json, bool withBom = false)
    {
        var body = Encoding.UTF8.GetBytes(json);
        if (!withBom) return body;
        return new byte[] { 0xEF, 0xBB, 0xBF }.Concat(body).ToArray();
    }

    [Fact]
    public void Valid_signature_is_accepted()
    {
        var payload = Payload("""{"id":"evt_1","type":"checkout.session.completed"}""");
        var ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        Assert.True(StripeWebhookVerifier.Verify(payload, Header(ts, Sign(Secret, ts, payload)), Secret));
    }

    [Fact]
    public void Payload_with_utf8_bom_is_verified_over_raw_bytes()
    {
        // Regresion: leer el cuerpo como texto elimina la BOM y la firma deja de
        // coincidir, rechazando webhooks legitimos.
        var payload = Payload("""{"id":"evt_2","type":"checkout.session.completed"}""", withBom: true);
        var ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        Assert.True(StripeWebhookVerifier.Verify(payload, Header(ts, Sign(Secret, ts, payload)), Secret));
    }

    [Fact]
    public void Non_ascii_payload_is_verified_over_raw_bytes()
    {
        var payload = Payload("""{"nombre":"Ñandú Ibérico €","tipo":"factura"}""");
        var ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        Assert.True(StripeWebhookVerifier.Verify(payload, Header(ts, Sign(Secret, ts, payload)), Secret));
    }

    [Fact]
    public void Tampered_payload_is_rejected()
    {
        var payload = Payload("""{"id":"evt_3","amount":100}""");
        var ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var signature = Header(ts, Sign(Secret, ts, payload));

        var tampered = Payload("""{"id":"evt_3","amount":1}""");

        Assert.False(StripeWebhookVerifier.Verify(tampered, signature, Secret));
    }

    [Fact]
    public void Signature_from_a_different_secret_is_rejected()
    {
        var payload = Payload("""{"id":"evt_4"}""");
        var ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        var signature = Header(ts, Sign("whsec_another_secret_999999999", ts, payload));

        Assert.False(StripeWebhookVerifier.Verify(payload, signature, Secret));
    }

    [Theory]
    [InlineData(301)]
    [InlineData(-301)]
    public void Stale_timestamp_is_rejected_to_block_replay(int skewSeconds)
    {
        var payload = Payload("""{"id":"evt_5"}""");
        var ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + skewSeconds;

        Assert.False(StripeWebhookVerifier.Verify(payload, Header(ts, Sign(Secret, ts, payload)), Secret));
    }

    [Fact]
    public void Fresh_timestamp_at_the_edge_is_accepted()
    {
        var payload = Payload("""{"id":"evt_6"}""");
        var ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - 290;

        Assert.True(StripeWebhookVerifier.Verify(payload, Header(ts, Sign(Secret, ts, payload)), Secret));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("garbage")]
    [InlineData("t=123")]
    [InlineData("v1=abcd")]
    public void Malformed_or_missing_signature_is_rejected(string? signature)
    {
        var payload = Payload("""{"id":"evt_7"}""");

        Assert.False(StripeWebhookVerifier.Verify(payload, signature, Secret));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Empty_secret_rejects_everything(string secret)
    {
        var payload = Payload("""{"id":"evt_8"}""");
        var ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        Assert.False(StripeWebhookVerifier.Verify(payload, Header(ts, Sign(Secret, ts, payload)), secret));
    }

    [Fact]
    public void Invalid_hex_signature_does_not_throw()
    {
        var payload = Payload("""{"id":"evt_9"}""");
        var ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var bogus = new string('z', 64);

        Assert.False(StripeWebhookVerifier.Verify(payload, Header(ts, bogus), Secret));
    }

    [Fact]
    public void Multiple_signatures_are_supported_for_secret_rotation()
    {
        var payload = Payload("""{"id":"evt_10"}""");
        var ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var header = Header(ts,
            Sign("whsec_previous_secret_00000000", ts, payload),
            Sign(Secret, ts, payload));

        Assert.True(StripeWebhookVerifier.Verify(payload, header, Secret));
    }
}