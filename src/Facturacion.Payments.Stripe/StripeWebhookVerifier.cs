using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Facturacion.Payments.Stripe;

public static class StripeWebhookVerifier
{
    /// <summary>
    /// Verifica la firma sobre los BYTES EXACTOS del cuerpo. Stripe firma el
    /// payload original: releerlo como texto y volver a codificar puede alterar
    /// los bytes (por ejemplo al eliminar una BOM) y rechazar webhooks validos.
    /// </summary>
    public static bool Verify(
        ReadOnlySpan<byte> payload, string? signature, string secret)
    {
        if (string.IsNullOrWhiteSpace(signature) || string.IsNullOrWhiteSpace(secret))
            return false;

        string? timestamp = null;
        var signatures = new List<string>();
        foreach (var part in signature.Split(','))
        {
            var item = part.Trim().Split('=', 2);
            if (item.Length != 2) continue;
            if (item[0] == "t") timestamp = item[1];
            if (item[0] == "v1") signatures.Add(item[1]);
        }

        if (string.IsNullOrEmpty(timestamp)) return false;
        if (!long.TryParse(timestamp, NumberStyles.None, CultureInfo.InvariantCulture, out var seconds))
            return false;
        if (Math.Abs(DateTimeOffset.UtcNow.ToUnixTimeSeconds() - seconds) > 300) return false;

        var signedPayload = new byte[Encoding.UTF8.GetByteCount(timestamp) + 1 + payload.Length];
        var prefix = Encoding.UTF8.GetBytes(timestamp + ".");
        prefix.CopyTo(signedPayload, 0);
        payload.CopyTo(signedPayload.AsSpan(prefix.Length));

        var expected = HMACSHA256.HashData(
            Encoding.UTF8.GetBytes(secret), signedPayload);

        foreach (var hex in signatures)
        {
            if (hex.Length != 64) continue;
            byte[] candidate;
            try { candidate = Convert.FromHexString(hex); }
            catch (FormatException) { continue; }

            if (CryptographicOperations.FixedTimeEquals(expected, candidate))
                return true;
        }

        return false;
    }
}