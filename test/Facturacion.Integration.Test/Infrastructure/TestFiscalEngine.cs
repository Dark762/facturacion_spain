using Facturacion.Verifactu;
using Facturacion.Verifactu.Canonicalization;
using Facturacion.Verifactu.Chain;
using Facturacion.Verifactu.Fingerprints;
using Facturacion.Verifactu.Qr;
using Facturacion.Verifactu.Validation;
using Facturacion.Verifactu.Xml;

namespace Facturacion.Integration.Test.Infrastructure;

/// <summary>
/// Motor fiscal de DOBLE para pruebas.
///
/// NO es una implementacion normativa. Existe unicamente para que las pruebas
/// de numeracion, autorizacion y aislamiento puedan ejercitar el camino de
/// emision completo, ya que el motor real esta bloqueado a proposito.
///
/// La canonicalizacion que usa concatena campos marcados como de prueba. Si
/// alguien toma este doble por una implementacion real, el resultado seran
/// huellas que la AEAT rechazaria: por eso el nombre lo dice en mayusculas.
/// </summary>
public sealed class TestRecordCanonicalizer : IRecordCanonicalizer
{
    public bool IsAvailable => true;

    public string BuildCanonicalString(CanonicalizationRequest r) =>
        string.Join('|',
            "PRUEBA-NO-NORMATIVO",
            r.IssuerNif,
            r.InvoiceNumber,
            r.IssueDate,
            r.InvoiceType,
            r.TaxTotal,
            r.TotalAmount,
            r.PreviousHash ?? string.Empty);
}

/// <summary>Motor fiscal marcado como listo, solo para pruebas.</summary>
public sealed class TestFiscalEngine : IFiscalEngine
{
    private readonly IVerifier _verifier = new Verifier();
    private readonly IFiscalChainWalker _chain = new FiscalChainWalker();
    private readonly IRecordXmlBuilder _xml = new AeatRecordXmlBuilder();
    private readonly IXsdValidator _xsd = new AeatXsdValidator(Path.Combine(AppContext.BaseDirectory, "xsd"));
    private readonly IQrGenerator _qr = new UnavailableQrGenerator();
    private readonly IFingerprintCalculator _fingerprint;

    public TestFiscalEngine()
    {
        var canonicalizer = new TestRecordCanonicalizer();
        _fingerprint = new Sha256FingerprintCalculator(canonicalizer);
    }

    public bool IsReadyForIssuance => true;

    public IReadOnlyList<string> Blockers => Array.Empty<string>();

    public string ReadinessReport => "MOTOR DE PRUEBA. No es una implementacion normativa.";

    public IVerifier Verifier => _verifier;
    public IFingerprintCalculator Fingerprint => _fingerprint;
    public IFiscalChainWalker Chain => _chain;
    public IRecordXmlBuilder Xml => _xml;
    public IXsdValidator Xsd => _xsd;
    public IQrGenerator Qr => _qr;
}

/// <summary>
/// Motor fiscal real, con la canonicalizacion no disponible.
/// Sirve para probar que la emision se bloquea mientras falte la especificacion.
/// </summary>
public sealed class BlockedFiscalEngine : IFiscalEngine
{
    private readonly IFiscalEngine _inner = new FiscalEngine(
        new Verifier(),
        new AeatRecordCanonicalizer(),
        new Sha256FingerprintCalculator(new AeatRecordCanonicalizer()),
        new FiscalChainWalker(),
        new AeatRecordXmlBuilder(),
        new AeatXsdValidator(Path.Combine(AppContext.BaseDirectory, "xsd")),
        new UnavailableQrGenerator());

    public bool IsReadyForIssuance => _inner.IsReadyForIssuance;
    public IReadOnlyList<string> Blockers => _inner.Blockers;
    public string ReadinessReport => _inner.ReadinessReport;
    public IVerifier Verifier => _inner.Verifier;
    public IFingerprintCalculator Fingerprint => _inner.Fingerprint;
    public IFiscalChainWalker Chain => _inner.Chain;
    public IRecordXmlBuilder Xml => _inner.Xml;
    public IXsdValidator Xsd => _inner.Xsd;
    public IQrGenerator Qr => _inner.Qr;
}