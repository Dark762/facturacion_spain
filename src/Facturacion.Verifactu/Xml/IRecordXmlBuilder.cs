using System.Globalization;
using System.Text;
using System.Xml;

namespace Facturacion.Verifactu.Xml;

/// <summary>Datos del emisor y del destinatario de un registro.</summary>
public sealed record PartyData(string Nif, string Name);

/// <summary>
/// Datos mínimos para construir un RegistroAlta o un RegistroAnulacion.
///
/// El constructor NO lee el esquema: recibe datos ya validados por
/// FiscalRecordService. Su trabajo es reflejar la estructura y el ORDEN que
/// exige el XSD.
/// </summary>
public sealed record FiscalRecordXmlData
{
    public string IdVersion { get; init; } = "1.0";

    // IDFactura
    public required string IssuerNif { get; init; }
    public required string InvoiceNumber { get; init; }   // "serie/numero"
    public required DateOnly IssueDate { get; init; }

    public required string IssuerName { get; init; }       // NombreRazonEmisor, obligatorio

    public required string InvoiceType { get; init; }     // F1, R1...

    /// <summary>DescripcionOperacion es obligatoria segun el esquema.</summary>
    public required string Description { get; init; }

    // Encadenamiento
    public bool IsFirstRecord { get; init; }
    public string? PreviousHash { get; init; }
    public string? PreviousIssuerNif { get; init; }
    public string? PreviousInvoiceNumber { get; init; }
    public DateOnly? PreviousIssueDate { get; init; }

    // Identificacion del sistema
    public required string SystemName { get; init; }
    public required string SystemId { get; init; }
    public required string SystemVersion { get; init; }
    public required string InstallationNumber { get; init; }
    public required string ProducerName { get; init; }
    public required string ProducerNif { get; init; }

    // Desglose
    public required IReadOnlyList<TaxBreakdownLine> TaxLines { get; init; }
    public required decimal TaxTotal { get; init; }
    public required decimal Total { get; init; }

    public required string Hash { get; init; }
    public required DateTimeOffset GeneratedAt { get; init; }
}

/// <summary>
/// Una linea del desglose fiscal.
///
/// CalificacionOperacion (AEAT, "Calificacion del tipo de operacion
/// Sujeta/No Exenta"):
///   S1 = Sujeta, no exenta, SIN inversion del sujeto pasivo  (caso normal con IVA)
///   S2 = Sujeta, no exenta, CON inversion del sujeto pasivo  (el cliente declara)
///   N1 = No sujeta (arts. 7, 14 LIVA y otros)
///   N2 = No sujeta por reglas de localizacion
/// El valor por defecto es S1 porque es el caso general de una venta con IVA.
/// </summary>
public sealed record TaxBreakdownLine(
    string TaxCode,
    decimal BaseAmount,
    decimal TaxAmount,
    decimal TaxRate)
{
    /// <summary>S1, S2, N1 o N2.</summary>
    public string CalificacionOperacion { get; init; } = "S1";
}

/// <summary>
/// Construye el XML del registro fiscal.
///
/// Nunca se concatenan cadenas: se usa XmlWriter, que escapa el contenido y
/// garantiza un documento bien formado. El orden de los elementos NO es
/// libre: el esquema declara una sequence, y un orden distinto es invalidado
/// aunque todos los datos sean correctos.
/// </summary>
public interface IRecordXmlBuilder
{
    bool IsAvailable { get; }

    /// <summary>Namespace del registro de alta/anulacion (SuministroInformacion.xsd).</summary>
    string RecordNamespace { get; }

    string BuildAltaXml(FiscalRecordXmlData data);

    string BuildAnulacionXml(FiscalRecordXmlData data);

    /// <summary>
    /// Envoltura de remision. El esquema NO define RegistroAlta como raiz: la
    /// raiz es RegFactuSistemaFacturacion, con Cabecera y hasta 1000 registros.
    /// Un RegistroAlta suelto NO valida, y ese es el motivo de existir este
    /// metodo.
    /// </summary>
    string BuildSubmissionXml(string issuerName, string issuerNif, string recordXml);
}

public sealed class AeatRecordXmlBuilder : IRecordXmlBuilder
{
    public const string SuministroInformacionNs =
        "https://www2.agenciatributaria.gob.es/static_files/common/internet/dep/aplicaciones/es/aeat/tike/cont/ws/SuministroInformacion.xsd";

    public bool IsAvailable => true;

    public string RecordNamespace => SuministroInformacionNs;

    private static string Fecha(DateOnly d) => d.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture);

    /// <summary>
    /// FechaHoraHusoGenRegistro. La huella se calcula con el MISMO valor, con su
    /// desplazamiento horario: si difieren, la huella es correcta y el documento
    /// inconsistente.
    /// </summary>
    private static string FechaHora(DateTimeOffset value) =>
        value.ToString("yyyy-MM-ddTHH:mm:sszzz", CultureInfo.InvariantCulture);

    /// <summary>Importe con punto decimal y sin separador de miles.</summary>
    private static string Importe(decimal value) =>
        value.ToString("0.00", CultureInfo.InvariantCulture);

    public string BuildAltaXml(FiscalRecordXmlData data) => Build(data, isAnulacion: false);

    public string BuildAnulacionXml(FiscalRecordXmlData data) => Build(data, isAnulacion: true);

    /// <summary>
    /// Envuelve el registro en RegFactuSistemaFacturacion con su Cabecera.
    ///
    /// No se concatenan cadenas: se le el fragmento con XmlReader y se copia
    /// nodo a nodo con WriteNode, que conserva namespace y contenido sin
    /// reanalizar texto.
    /// </summary>
    public string BuildSubmissionXml(string issuerName, string issuerNif, string recordXml)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(issuerName);
        ArgumentException.ThrowIfNullOrWhiteSpace(issuerNif);
        ArgumentException.ThrowIfNullOrWhiteSpace(recordXml);

        const string SuministroLrNs =
            "https://www2.agenciatributaria.gob.es/static_files/common/internet/dep/aplicaciones/es/aeat/tike/cont/ws/SuministroLR.xsd";

        using var stream = new MemoryStream();
        var settings = new XmlWriterSettings
        {
            Indent = true,
            Encoding = new UTF8Encoding(false),
            OmitXmlDeclaration = false
        };

        using (var writer = XmlWriter.Create(stream, settings))
        {
            writer.WriteStartElement("RegFactuSistemaFacturacion", SuministroLrNs);
            writer.WriteAttributeString("xmlns", "sf", null, SuministroInformacionNs);

            // Cabecera: se declara en SuministroLR.xsd, asi que pertenece a SU namespace.
            // Solo su CONTENIDO (ObligadoEmision) vive en SuministroInformacion.
            // ObligadoEmision es obligatorio, el resto opcional.
            writer.WriteStartElement("Cabecera", SuministroLrNs);
            writer.WriteStartElement("ObligadoEmision", SuministroInformacionNs);
            writer.WriteElementString("NombreRazon", SuministroInformacionNs, issuerName);
            writer.WriteElementString("NIF", SuministroInformacionNs, issuerNif);
            writer.WriteEndElement();   // ObligadoEmision
            writer.WriteEndElement();   // Cabecera (namespace SuministroLR)

            // RegistroFactura: se copia SOLO el elemento raiz del fragmento.
            // El fragmento es un documento completo (declaracion XML incluida),
            // y escribirlo entero aqui cerraria la raiz antes de tiempo.
            writer.WriteStartElement("RegistroFactura", SuministroLrNs);
            var fragment = System.Xml.Linq.XDocument.Parse(
                recordXml,
                System.Xml.Linq.LoadOptions.SetLineInfo);
            using (var source = fragment.Root!.CreateReader())
            {
                source.Read();   // se posiciona en el elemento
                writer.WriteNode(source, defattr: true);
            }
            writer.WriteEndElement();   // RegistroFactura

            writer.WriteEndElement();   // RegFactuSistemaFacturacion
            writer.Flush();
        }

        return System.Text.Encoding.UTF8.GetString(stream.ToArray());
    }

    private string Build(FiscalRecordXmlData data, bool isAnulacion)
    {
        ArgumentNullException.ThrowIfNull(data);

        using var stream = new MemoryStream();
        var settings = new XmlWriterSettings
        {
            Indent = true,
            Encoding = new UTF8Encoding(false),   // UTF-8 sin BOM, como exige la AEAT
            OmitXmlDeclaration = false
        };

        using (var writer = XmlWriter.Create(stream, settings))
        {
            writer.WriteStartElement(isAnulacion ? "RegistroAnulacion" : "RegistroAlta", SuministroInformacionNs);

            // --- orden segun RegistroFacturacionAltaType ---
            writer.WriteElementString("IDVersion", SuministroInformacionNs, data.IdVersion);

            // IDFactura
            writer.WriteStartElement("IDFactura", SuministroInformacionNs);
            writer.WriteElementString("IDEmisorFactura", SuministroInformacionNs, data.IssuerNif);
            writer.WriteElementString("NumSerieFactura", SuministroInformacionNs, data.InvoiceNumber);
            writer.WriteElementString("FechaExpedicionFactura", SuministroInformacionNs, Fecha(data.IssueDate));
            writer.WriteEndElement();

            writer.WriteElementString("NombreRazonEmisor", SuministroInformacionNs, data.IssuerName);

            writer.WriteElementString("TipoFactura", SuministroInformacionNs, data.InvoiceType);

            // DescripcionOperacion es obligatoria (posicion 13 del esquema).
            writer.WriteElementString("DescripcionOperacion", SuministroInformacionNs, data.Description);

            // Desglose
            writer.WriteStartElement("Desglose", SuministroInformacionNs);
            foreach (var line in data.TaxLines)
            {
                writer.WriteStartElement("DetalleDesglose", SuministroInformacionNs);
                // Impuesto es un elemento de TEXTO con el codigo impositivo (01=IVA).
                writer.WriteElementString("Impuesto", SuministroInformacionNs, line.TaxCode);
                writer.WriteElementString("ClaveRegimen", SuministroInformacionNs, "01");

                // El esquema obliga a elegir CalificacionOperacion u OperacionExenta.
                writer.WriteElementString("CalificacionOperacion", SuministroInformacionNs,
                    line.CalificacionOperacion);

                // Con inversion del sujeto pasivo (S2) la AEAT exige informar
                // tipo impositivo y cuota repercutida a cero: no se repercute aqui.
                var invierte = line.CalificacionOperacion == "S2";

                writer.WriteElementString("TipoImpositivo", SuministroInformacionNs,
                    invierte ? "0.00" : Importe(line.TaxRate));
                writer.WriteElementString("BaseImponibleOimporteNoSujeto", SuministroInformacionNs,
                    Importe(line.BaseAmount));
                writer.WriteElementString("CuotaRepercutida", SuministroInformacionNs,
                    invierte ? "0.00" : Importe(line.TaxAmount));
                writer.WriteEndElement();   // DetalleDesglose
            }
            writer.WriteEndElement();       // Desglose

            writer.WriteElementString("CuotaTotal", SuministroInformacionNs, Importe(data.TaxTotal));
            writer.WriteElementString("ImporteTotal", SuministroInformacionNs, Importe(data.Total));

            // Encadenamiento
            writer.WriteStartElement("Encadenamiento", SuministroInformacionNs);
            if (data.IsFirstRecord)
            {
                writer.WriteElementString("PrimerRegistro", SuministroInformacionNs, "S");
            }
            else
            {
                writer.WriteStartElement("RegistroAnterior", SuministroInformacionNs);
                writer.WriteElementString("IDEmisorFactura", SuministroInformacionNs,
                    data.PreviousIssuerNif ?? data.IssuerNif);
                writer.WriteElementString("NumSerieFactura", SuministroInformacionNs,
                    data.PreviousInvoiceNumber ?? data.InvoiceNumber);
                writer.WriteElementString("FechaExpedicionFactura", SuministroInformacionNs,
                    Fecha(data.PreviousIssueDate ?? data.IssueDate));
                writer.WriteElementString("Huella", SuministroInformacionNs, data.PreviousHash ?? "");
                writer.WriteEndElement();
            }
            writer.WriteEndElement();       // Encadenamiento

            // SistemaInformatico
            writer.WriteStartElement("SistemaInformatico", SuministroInformacionNs);
            writer.WriteElementString("NombreRazon", SuministroInformacionNs, data.ProducerName);
            writer.WriteElementString("NIF", SuministroInformacionNs, data.ProducerNif);
            writer.WriteElementString("NombreSistemaInformatico", SuministroInformacionNs, data.SystemName);
            writer.WriteElementString("IdSistemaInformatico", SuministroInformacionNs, data.SystemId);
            writer.WriteElementString("Version", SuministroInformacionNs, data.SystemVersion);
            writer.WriteElementString("NumeroInstalacion", SuministroInformacionNs, data.InstallationNumber);
            writer.WriteElementString("TipoUsoPosibleSoloVerifactu", SuministroInformacionNs, "N");
            writer.WriteElementString("TipoUsoPosibleMultiOT", SuministroInformacionNs, "N");
            writer.WriteElementString("IndicadorMultiplesOT", SuministroInformacionNs, "N");
            writer.WriteEndElement();       // SistemaInformatico

            writer.WriteElementString("FechaHoraHusoGenRegistro", SuministroInformacionNs,
                FechaHora(data.GeneratedAt));

            writer.WriteElementString("TipoHuella", SuministroInformacionNs, "01");
            writer.WriteElementString("Huella", SuministroInformacionNs, data.Hash);

            writer.WriteEndElement();       // RegistroAlta / RegistroAnulacion
            writer.Flush();
        }

        return System.Text.Encoding.UTF8.GetString(stream.ToArray());
    }
}