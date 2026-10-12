namespace Facturacion.Verifactu;

using System.Collections.Immutable;
using System.Reflection;

/// <summary>
/// Estado de cada componente del motor fiscal y por que puede estar no disponible.
/// </summary>
public enum FiscalComponentStatus
{
    Available,

    /// <summary>Implementado y verificado contra la especificacion oficial.</summary>
    Available_Verified,

    /// <summary>Implementado pero sin verificacion oficial.</summary>
    Implemented_Unverified,

    /// <summary>No implementado porque falta el requisito normativo.</summary>
    Unavailable_MissingSpecification,

    /// <summary>No implementado por decision de alcance.</summary>
    Unavailable_OutOfScope
}

public sealed record FiscalComponent(
    string Name,
    FiscalComponentStatus Status,
    string Reason,
    string? Reference = null)
{
    public bool CanBeUsed => Status is FiscalComponentStatus.Available
                                or FiscalComponentStatus.Available_Verified;
}

/// <summary>
/// Unica fuente de verdad sobre que sabe y que no sabe hacer el motor fiscal.
/// La emision consulta esta tabla y se bloquea si falta algun componente obligatorio.
/// </summary>
public static class FiscalSpecification
{
    public const string ReferenceUrl =
        "https://sede.agenciatributaria.gob.es/Sede/iva/sistemas-informaticos-facturacion-verifactu/informacion-tecnica.html";

    public const string HashDocumentUrl =
        "https://www.agenciatributaria.es/static_files/AEAT_Desarrolladores/EEDD/IVA/VERI-FACTU/Veri-Factu_especificaciones_huella_hash_registros.pdf";

    /// <summary>Consultado el 2026-10-12. Pagina de la AEAT actualizada el 2026-10-07.</summary>
    public const string ConsultedOn = "2026-10-12";

    /// <summary>
    /// Version del documento oficial. Leida del propio PDF, no de memoria.
    /// Importa fijarla: el documento tiene historial de revisiones y el
    /// formato de los importes y los ejemplos ha cambiado entre ellas.
    /// </summary>
    public const string HashSpecificationVersion = "0.1.2";

    public const string HashSpecificationDate = "2024-08-27";

    /// <summary>
    /// Confirmado en la FAQ oficial de la AEAT (actualizada el 2026-07-21):
    /// el algoritmo de huella es SHA-256 para todos los tipos de registro.
    ///
    /// Confirmado ademas en el apartado 2 del documento oficial:
    /// "el unico algoritmo permitido es SHA-256".
    /// La salida es hexadecimal EN MAYUSCULAS de 64 caracteres.
    /// </summary>
    public const string FingerprintAlgorithm = "SHA256";

    /// <summary>La huella del registro anterior se encadena en el siguiente. Confirmado en la FAQ oficial.</summary>
    public const bool ChainingRequired = true;

    public static ImmutableArray<FiscalComponent> Components { get; } =
        ImmutableArray.Create(
            new FiscalComponent(
                nameof(Validation.Verifier), FiscalComponentStatus.Available,
                "Reglas de negocio internas del proyecto; no derivan de la norma."),

            new FiscalComponent(
                nameof(Records.AltaRecordBuilder), FiscalComponentStatus.Available,
                "Construye el modelo del registro de alta. No serializa a XML."),

            new FiscalComponent(
                nameof(Records.AnulacionRecordBuilder), FiscalComponentStatus.Available,
                "Construye el modelo del registro de anulacion. No serializa a XML."),

            new FiscalComponent(
                nameof(Canonicalization.IRecordCanonicalizer), FiscalComponentStatus.Available_Verified,
                "Cadena canonica verificada contra la especificacion oficial v" + HashSpecificationVersion +
                " (" + HashSpecificationDate + "): pares nombreCampo=valor separados por '&', en el " +
                "orden de campos de cada tipo de registro, valores recortados y codificacion UTF-8. " +
                "Los tres vectores oficiales reproducen exactamente.",
                HashDocumentUrl),

            new FiscalComponent(
                nameof(Fingerprints.Sha256FingerprintCalculator), FiscalComponentStatus.Available_Verified,
                "SHA-256 verificado: los tres casos oficiales (alta inicial, alta encadenada y " +
                "anulacion) reproducen la huella esperada al byte.",
                HashDocumentUrl),

            new FiscalComponent(
                nameof(Chain.FiscalChainWalker), FiscalComponentStatus.Available,
                "Encadenamiento y verificacion de la cadena. Usa el algoritmo ya confirmado."),

            new FiscalComponent(
                nameof(Xml.IRecordXmlBuilder), FiscalComponentStatus.Unavailable_MissingSpecification,
                "La estructura XML debe validar contra los XSD oficiales, que no se han descargado " +
                "ni contrastado en esta entrega.",
                ReferenceUrl),

            new FiscalComponent(
                nameof(Xml.IXsdValidator), FiscalComponentStatus.Unavailable_MissingSpecification,
                "Requiere los esquemas XSD oficiales de la AEAT.",
                ReferenceUrl),

            new FiscalComponent(
                nameof(Qr.IQrGenerator), FiscalComponentStatus.Unavailable_MissingSpecification,
                "El formato del QR y sus parametros no se han verificado. La FAQ oficial confirma " +
                "que la huella NO forma parte del QR, pero el resto de parametros siguen sin verificar.",
                ReferenceUrl));

    /// <summary>Componentes sin los cuales no se puede emitir.</summary>
    public static ImmutableArray<string> MandatoryComponents { get; } =
        ImmutableArray.Create(
            nameof(Validation.Verifier),
            nameof(Records.AltaRecordBuilder),
            nameof(Canonicalization.IRecordCanonicalizer),
            nameof(Fingerprints.Sha256FingerprintCalculator),
            nameof(Chain.FiscalChainWalker),
            nameof(Xml.IRecordXmlBuilder),
            nameof(Xml.IXsdValidator));

    /// <summary>Componentes que bloquean la emision porque no se han verificado.</summary>
    public static ImmutableArray<string> BlockingComponents { get; } =
        MandatoryComponents
            .Where(c => !Components.First(x => x.Name == c).CanBeUsed)
            .ToImmutableArray();

    /// <summary>
    /// True solo si todos los componentes obligatorios estan disponibles.
    /// Mientras sea false, la emision debe rechazarse.
    /// </summary>
    public static bool IsReadyForIssuance => BlockingComponents.IsEmpty;

    public static string BuildReadinessReport()
    {
        var lines = new List<string>
        {
            $"Especificacion AEAT consultada el {ConsultedOn}.",
            $"Algoritmo de huella confirmado por la FAQ oficial: {FingerprintAlgorithm}.",
            $"Emision habilitada: {(IsReadyForIssuance ? "SI" : "NO")}.",
            string.Empty
        };

        foreach (var c in Components)
            lines.Add($"  {c.Status,-38} {c.Name}");

        if (!BlockingComponents.IsEmpty)
        {
            lines.Add(string.Empty);
            lines.Add("Componentes que bloquean la emision:");
            foreach (var b in BlockingComponents)
                lines.Add($"  - {b}");
        }

        return string.Join(Environment.NewLine, lines);
    }
}