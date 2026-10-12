namespace Facturacion.Verifactu.Canonicalization;

/// <summary>Tipo de registro fiscal. Determina los nombres de campo de la cadena canonica.</summary>
public enum FiscalRecordKind
{
    Alta,
    Anulacion
}

/// <summary>
/// Datos de entrada de la huella, tal y como aparecen en el registro XML.
///
/// Los nombres NO coinciden con los del dominio a proposito: la especificacion
/// exige el nombre del campo tal como aparece en el XSD, y ese nombre cambia
/// entre alta y anulacion (IDEmisorFactura frente a IDEmisorFacturaAnulada).
/// Por eso el mapeo vive aqui y no en el consumidor.
///
/// Verificado contra el documento oficial de la AEAT v0.1.2 (27/08/2024).
/// </summary>
/// <param name="IssueDate">Fecha de expedicion ya en formato dd-MM-yyyy.</param>
/// <param name="TaxTotal">CuotaTotal: sumatorio de cuotas.</param>
/// <param name="TotalAmount">ImporteTotal.</param>
/// <param name="PreviousHash">Huella del registro anterior. Null o vacio si es el primero.</param>
/// <param name="GeneratedAt">FechaHoraHusoGenRegistro con desplazamiento, p. ej. 2024-01-01T19:20:30+01:00.</param>
public sealed record CanonicalizationRequest(
    FiscalRecordKind Kind,
    string? IssuerNif,
    string? InvoiceNumber,
    string? IssueDate,
    string? InvoiceType,
    string? TaxTotal,
    string? TotalAmount,
    string? PreviousHash,
    string? GeneratedAt);

/// <summary>
/// Produce la cadena canonica sobre la que se calcula la huella.
///
/// Formato (apartado 3 del documento oficial): los campos se concatenan en el
/// orden enunciado para cada tipo de registro formando pares
/// nombreCampo=valor separados por '&amp;'.
///
/// Reglas verificadas:
///  - El nombre de campo es una constante tal como aparece en el XML.
///  - Los valores se recortan de espacios al inicio y al final.
///  - En numericos da igual una o dos decimales y los ceros a la derecha no
///    son significativos.
///  - Si el campo no aparece o esta vacio, el par se conserva con valor vacio:
///    "Huella=". Omitirlo produciria una huella distinta.
///  - La cadena se codifica en UTF-8 antes de aplicar el algoritmo.
/// </summary>
public interface IRecordCanonicalizer
{
    bool IsAvailable { get; }

    string BuildCanonicalString(CanonicalizationRequest request);
}

/// <summary>
/// Ya no se usa: la canonicalizacion esta implementada y verificada contra la
/// especificacion oficial. Se conserva el tipo por si hubiera que volver a
/// bloquear el componente.
/// </summary>
public sealed class CanonicalizationUnavailableException()
    : InvalidOperationException(
        "La canonicalizacion del registro fiscal no esta disponible.")
{
}

/// <summary>
/// Implementacion contra la especificacion oficial de la AEAT v0.1.2.
/// Verificada con los tres vectores publicados por la Agencia Tributaria.
/// </summary>
public sealed class AeatRecordCanonicalizer : IRecordCanonicalizer
{
    public bool IsAvailable => true;

    public string BuildCanonicalString(CanonicalizationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        // El orden NO es negociable: forma parte de la norma.
        (string Name, string? Value)[] pairs =
            request.Kind == FiscalRecordKind.Anulacion
                ?
                [
                    ("IDEmisorFacturaAnulada", request.IssuerNif),
                    ("NumSerieFacturaAnulada", request.InvoiceNumber),
                    ("FechaExpedicionFacturaAnulada", request.IssueDate),
                    ("Huella", request.PreviousHash),
                    ("FechaHoraHusoGenRegistro", request.GeneratedAt)
                ]
                :
                [
                    ("IDEmisorFactura", request.IssuerNif),
                    ("NumSerieFactura", request.InvoiceNumber),
                    ("FechaExpedicionFactura", request.IssueDate),
                    ("TipoFactura", request.InvoiceType),
                    ("CuotaTotal", request.TaxTotal),
                    ("ImporteTotal", request.TotalAmount),
                    ("Huella", request.PreviousHash),
                    ("FechaHoraHusoGenRegistro", request.GeneratedAt)
                ];

        return string.Join('&', pairs.Select(p => $"{p.Name}={Clean(p.Value)}"));
    }

    /// <summary>
    /// Recorta y normaliza a null lo que no tiene contenido. Un par con valor
    /// vacio se conserva: "Huella=" es lo correcto para un primer registro.
    /// </summary>
    private static string Clean(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        return value.Trim();
    }
}