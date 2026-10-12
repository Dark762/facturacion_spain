namespace Facturacion.Verifactu;

using Facturacion.Verifactu.Canonicalization;
using Facturacion.Verifactu.Chain;
using Facturacion.Verifactu.Fingerprints;
using Facturacion.Verifactu.Qr;
using Facturacion.Verifactu.Records;
using Facturacion.Verifactu.Validation;
using Facturacion.Verifactu.Xml;

/// <summary>
/// Fachada del motor fiscal. Es el unico punto que la aplicacion consulta.
/// </summary>
public interface IFiscalEngine
{
    /// <summary>False mientras falte cualquier componente obligatorio.</summary>
    bool IsReadyForIssuance { get; }

    /// <summary>Componentes que impiden emitir, con su motivo.</summary>
    IReadOnlyList<string> Blockers { get; }

    /// <summary>Informe legible del estado del motor.</summary>
    string ReadinessReport { get; }

    IVerifier Verifier { get; }
    IFingerprintCalculator Fingerprint { get; }
    IFiscalChainWalker Chain { get; }
    IRecordXmlBuilder Xml { get; }
    IXsdValidator Xsd { get; }
    IQrGenerator Qr { get; }
}

public sealed class FiscalEngine : IFiscalEngine
{
    public FiscalEngine(
        IVerifier verifier,
        IRecordCanonicalizer canonicalizer,
        IFingerprintCalculator fingerprint,
        IFiscalChainWalker chain,
        IRecordXmlBuilder xml,
        IXsdValidator xsd,
        IQrGenerator qr)
    {
        Verifier = verifier;
        Fingerprint = fingerprint;
        Chain = chain;
        Xml = xml;
        Xsd = xsd;
        Qr = qr;
    }

    public IVerifier Verifier { get; }
    public IFingerprintCalculator Fingerprint { get; }
    public IFiscalChainWalker Chain { get; }
    public IRecordXmlBuilder Xml { get; }
    public IXsdValidator Xsd { get; }
    public IQrGenerator Qr { get; }

    public bool IsReadyForIssuance => FiscalSpecification.IsReadyForIssuance;

    public IReadOnlyList<string> Blockers => FiscalSpecification.BlockingComponents.ToArray();

    public string ReadinessReport => FiscalSpecification.BuildReadinessReport();
}