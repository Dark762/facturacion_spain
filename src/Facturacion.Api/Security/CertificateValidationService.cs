using System.Formats.Asn1;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Facturacion.Api.Security;

/// <summary>
/// Validación de certificado de cliente con anclas de confianza explícitas.
/// No autentica la identidad fiscal ni la representación de una sociedad.
/// </summary>
public sealed class CertificateValidationService(IConfiguration configuration)
{
    public sealed record CertificateAttributes(
        string CommonName, string? GivenName, string? Surname,
        string? SerialNumber, string? OrganizationIdentifier,
        string Issuer, string CertificateSerial, string Sha256Thumbprint,
        DateTimeOffset ExpiresAt);

    public sealed record ValidationResult(bool Valid, string Code, CertificateAttributes? Attributes);

    public ValidationResult Validate(X509Certificate2 certificate)
    {
        var roots = configuration.GetSection("IdentityCertificates:TrustedRootPaths").Get<string[]>() ?? [];
        if (roots.Length == 0)
            return new(false, "CERTIFICATE_TRUST_NOT_CONFIGURED", null);

        var now = DateTimeOffset.UtcNow;
        if (now < certificate.NotBefore.ToUniversalTime() || now >= certificate.NotAfter.ToUniversalTime())
            return new(false, "CERTIFICATE_EXPIRED_OR_NOT_YET_VALID", null);

        // Solo se admite certificado de autenticación cliente cuando declara EKU.
        var eku = certificate.Extensions.OfType<X509EnhancedKeyUsageExtension>().FirstOrDefault();
        if (eku is not null && !eku.EnhancedKeyUsages.Cast<Oid>()
                .Any(x => x.Value == "1.3.6.1.5.5.7.3.2"))
            return new(false, "CERTIFICATE_CLIENT_AUTH_EKU_MISSING", null);

        using var chain = new X509Chain();
        chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
        chain.ChainPolicy.RevocationMode = X509RevocationMode.Online;
        chain.ChainPolicy.RevocationFlag = X509RevocationFlag.ExcludeRoot;
        chain.ChainPolicy.VerificationFlags = X509VerificationFlags.NoFlag;
        chain.ChainPolicy.UrlRetrievalTimeout = TimeSpan.FromSeconds(8);
        try
        {
            foreach (var path in roots)
            {
                // Rutas de certificados solo desde configuración de servidor, nunca del usuario.
                var root = X509Certificate2.CreateFromPemFile(path);
                chain.ChainPolicy.CustomTrustStore.Add(root);
            }
            if (!chain.Build(certificate))
                return new(false, "CERTIFICATE_CHAIN_OR_REVOCATION_INVALID", null);

            var fields = ReadSubject(certificate.SubjectName.RawData);
            fields.TryGetValue("2.5.4.3", out var cn);
            fields.TryGetValue("2.5.4.42", out var given);
            fields.TryGetValue("2.5.4.4", out var surname);
            fields.TryGetValue("2.5.4.5", out var serial);
            fields.TryGetValue("2.5.4.97", out var organization);
            if (string.IsNullOrWhiteSpace(cn))
                return new(false, "CERTIFICATE_SUBJECT_INCOMPLETE", null);

            var attrs = new CertificateAttributes(cn, given, surname, serial, organization,
                certificate.Issuer, certificate.SerialNumber,
                Convert.ToHexString(SHA256.HashData(certificate.RawData)),
                new DateTimeOffset(certificate.NotAfter.ToUniversalTime()));
            return new(true, "CERTIFICATE_CRYPTOGRAPHICALLY_VALID", attrs);
        }
        catch (Exception ex) when (ex is CryptographicException or IOException or AsnContentException or ArgumentException)
        {
            return new(false, "CERTIFICATE_VALIDATION_ERROR", null);
        }
        finally
        {
            foreach (var root in chain.ChainPolicy.CustomTrustStore) root.Dispose();
        }
    }

    internal static Dictionary<string, string> ReadSubject(byte[] encoded)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        var reader = new AsnReader(encoded, AsnEncodingRules.DER);
        var sequence = reader.ReadSequence();
        while (sequence.HasData)
        {
            var set = sequence.ReadSetOf();
            while (set.HasData)
            {
                var attribute = set.ReadSequence();
                var oid = attribute.ReadObjectIdentifier();
                var tag = attribute.PeekTag();
                string? value = tag.TagClass == TagClass.Universal ? (UniversalTagNumber)tag.TagValue switch
                {
                    UniversalTagNumber.UTF8String => attribute.ReadCharacterString(UniversalTagNumber.UTF8String),
                    UniversalTagNumber.PrintableString => attribute.ReadCharacterString(UniversalTagNumber.PrintableString),
                    UniversalTagNumber.IA5String => attribute.ReadCharacterString(UniversalTagNumber.IA5String),
                    UniversalTagNumber.BMPString => attribute.ReadCharacterString(UniversalTagNumber.BMPString),
                    _ => null
                } : null;
                if (value is null) attribute.ReadEncodedValue();
                attribute.ThrowIfNotEmpty();
                if (value is not null && !values.ContainsKey(oid)) values.Add(oid, value);
            }
        }
        reader.ThrowIfNotEmpty();
        return values;
    }
}
