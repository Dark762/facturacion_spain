using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Facturacion.Api.Security;
using Microsoft.Extensions.Configuration;

namespace Facturacion.Integration.Test;

public sealed class IdentityCertificateValidationTests
{
    private static X509Certificate2 CreateSelfSigned(DateTimeOffset from, DateTimeOffset to)
    {
        using var key = RSA.Create(2048);
        var request = new CertificateRequest("CN=Prueba Autonomo, SERIALNUMBER=00000000T",
            key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));
        request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(
            new OidCollection { new Oid("1.3.6.1.5.5.7.3.2") }, true));
        return request.CreateSelfSigned(from, to);
    }

    [Fact]
    public void No_configured_trust_roots_must_fail_closed()
    {
        using var cert = CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
        var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var service = new CertificateValidationService(configuration);
        var result = service.Validate(cert);
        Assert.False(result.Valid);
        Assert.Equal("CERTIFICATE_TRUST_NOT_CONFIGURED", result.Code);
    }

    [Fact]
    public void Expired_certificate_is_rejected_even_with_trust_configured()
    {
        using var cert = CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-5), DateTimeOffset.UtcNow.AddDays(-2));
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["IdentityCertificates:TrustedRootPaths:0"] = "unused-test-root.pem"
        }).Build();
        var result = new CertificateValidationService(configuration).Validate(cert);
        Assert.False(result.Valid);
        Assert.Equal("CERTIFICATE_EXPIRED_OR_NOT_YET_VALID", result.Code);
    }

    [Fact]
    public void Untrusted_certificate_cannot_pass_validation()
    {
        using var cert = CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
        var path = Path.GetTempFileName();
        try
        {
            // Certificado raíz distinto del presentado: no debe admitirse.
            using var other = CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
            File.WriteAllText(path, other.ExportCertificatePem());
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["IdentityCertificates:TrustedRootPaths:0"] = path
            }).Build();
            var result = new CertificateValidationService(configuration).Validate(cert);
            Assert.False(result.Valid);
        }
        finally { File.Delete(path); }
    }
}
