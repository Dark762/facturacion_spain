using System.Xml;
using System.Xml.Schema;

namespace Facturacion.Verifactu.Xml;

/// <summary>Resultado de validar un documento contra los esquemas oficiales.</summary>
public sealed record XsdValidationResult(bool IsValid, IReadOnlyList<string> Errors)
{
    public string Summary => IsValid
        ? "XML conforme al esquema oficial de la AEAT."
        : $"{Errors.Count} incumplimiento(s) del esquema: " + string.Join(" | ", Errors);
}

/// <summary>
/// Valida un documento contra los XSD oficiales de la AEAT.
///
/// SEGURIDAD. No se resuelve ninguna referencia externa:
///
///   - XmlResolver queda en null. Los esquemas se cargan desde disco, nunca
///     desde la red.
///   - DtdProcessing queda en Prohibit, que es el valor por defecto y el que
///     impide entidades XML externas. Por eso el esquema xmldsig local tiene la
///     declaracion DOCTYPE eliminada: se prefiero modificar el fichero a relajar
///     la proteccion.
///   - El documento validado se lee con DtdProcessing.Prohibit.
///
/// VERSIONADO. El esquema aplicado queda registrado en el resultado para poder
/// demostrar que版本 contra la que se valido un documento concreto.
/// </summary>
public interface IXsdValidator
{
    bool IsAvailable { get; }

    /// <summary>Version del documento de huellas aplicada al cargar los esquemas.</summary>
    string SchemaReference { get; }

    XsdValidationResult Validate(string xml);
}

/// <summary>
/// Validador contra los XSD oficiales, con los esquemas ya descargados.
///
/// Dependencia externa: http://www.w3.org/TR/xmldsig-core/xmldsig-core-schema.xsd
/// La AEAT la importa por URL. Se carga una copia LOCAL precisamente para no
/// depender de la red al validar.
/// </summary>
public sealed class AeatXsdValidator : IXsdValidator
{
    public const string SuministroLrNs =
        "https://www2.agenciatributaria.gob.es/static_files/common/internet/dep/aplicaciones/es/aeat/tike/cont/ws/SuministroLR.xsd";

    public const string SuministroInformacionNs =
        "https://www2.agenciatributaria.gob.es/static_files/common/internet/dep/aplicaciones/es/aeat/tike/cont/ws/SuministroInformacion.xsd";

    public const string XmlDsigNs = "http://www.w3.org/2000/09/xmldsig#";

    private readonly Lazy<XmlSchemaSet> _schemas;

    public AeatXsdValidator(string schemaDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(schemaDirectory);
        _schemas = new Lazy<XmlSchemaSet>(() => Load(schemaDirectory), isThreadSafe: true);
    }

    public bool IsAvailable => true;

    public string SchemaReference =>
        "AEAT, Esquemas de los servicios web. Huella: documento oficial v0.1.2 (27/08/2024).";

    /// <summary>
    /// Carga los esquemas en el orden correcto. La firma XML va primero porque
    /// SuministroInformacion.xsd la importa.
    /// </summary>
    private static XmlSchemaSet Load(string directory)
    {
        var schemas = new XmlSchemaSet { XmlResolver = null };

        var signature = Path.Combine(directory, "xmldsig-core-schema.xsd");
        if (File.Exists(signature))
        {
            using var sigReader = XmlReader.Create(signature, SecureSettings());
            schemas.Add(XmlDsigNs, sigReader);
        }

        foreach (var file in new[] { "SuministroInformacion.xsd", "SuministroLR.xsd" })
        {
            var path = Path.Combine(directory, file);
            if (!File.Exists(path))
                throw new FileNotFoundException($"Falta el esquema oficial '{file}'.", path);

            using var reader = XmlReader.Create(path, SecureSettings());
            schemas.Add(null, reader);
        }

        schemas.Compile();
        return schemas;
    }

    private static XmlReaderSettings SecureSettings() => new()
    {
        XmlResolver = null,
        DtdProcessing = DtdProcessing.Prohibit
    };

    public XsdValidationResult Validate(string xml)
    {
        if (string.IsNullOrWhiteSpace(xml))
            return new XsdValidationResult(false, new[] { "El documento XML esta vacio." });

        var errors = new List<string>();

        var settings = new XmlReaderSettings
        {
            ValidationType = ValidationType.Schema,
            Schemas = _schemas.Value,
            DtdProcessing = DtdProcessing.Prohibit
        };
        settings.ValidationEventHandler += (_, e) =>
        {
            if (e.Severity == XmlSeverityType.Error) errors.Add(e.Message);
        };

        try
        {
            using var reader = XmlReader.Create(new StringReader(xml), settings);
            while (reader.Read()) { }
        }
        catch (XmlException ex)
        {
            errors.Add($"XML mal formado: {ex.Message}");
        }

        return new XsdValidationResult(errors.Count == 0, errors);
    }
}