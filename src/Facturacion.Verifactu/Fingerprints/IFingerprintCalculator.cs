namespace Facturacion.Verifactu.Fingerprints;

using System.Security.Cryptography;
using System.Text;
using Facturacion.Verifactu.Canonicalization;

/// <summary>Resultado de calcular la huella de un registro.</summary>
public sealed record FingerprintResult(string Hash, string Algorithm, string PreviousHash, bool Chained);

/// <summary>Calcula la huella de un registro fiscal.</summary>
public interface IFingerprintCalculator
{
    /// <summary>False si no puede usarse en produccion.</summary>
    bool IsAvailable { get; }

    FingerprintResult Compute(CanonicalizationRequest request);
}

/// <summary>
/// SHA-256 sobre la cadena canonica codificada en UTF-8.
///
/// Apartado 5 del documento oficial de la AEAT v0.1.2:
///   "El algoritmo de huella a utilizar es SHA-256"
///   "El formato de salida sera: en sistema hexadecimal, en mayusculas"
///   "El tamano sera de 64 caracteres alfanumericos".
///
/// La salida en MAYUSCULAS no es un detalle: el XSD limita Huella a
/// TextMax64Type y los vectores oficiales van en mayusculas.
/// </summary>
public sealed class Sha256FingerprintCalculator(IRecordCanonicalizer canonicalizer) : IFingerprintCalculator
{
    public const string AlgorithmName = "SHA256";

    public bool IsAvailable => canonicalizer.IsAvailable;

    public FingerprintResult Compute(CanonicalizationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var canonical = canonicalizer.BuildCanonicalString(request);
        var bytes = Encoding.UTF8.GetBytes(canonical);
        var hash = Convert.ToHexString(SHA256.HashData(bytes));   // ToHexString ya devuelve MAYUSCULAS

        return new FingerprintResult(
            hash,
            AlgorithmName,
            request.PreviousHash ?? string.Empty,
            !string.IsNullOrEmpty(request.PreviousHash));
    }

    /// <summary>
    /// Verificacion de cadena sin recalcular: comprueba que una huella tenga la
    /// forma que exige la norma, para descartar de entrada valores inventados.
    /// </summary>
    public static bool HasOfficialShape(string? hash) =>
        !string.IsNullOrEmpty(hash)
        && hash.Length == 64
        && hash.All(c => c is >= '0' and <= '9' or >= 'A' and <= 'F');
}