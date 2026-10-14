namespace Facturacion.Configuration.Options;

/// <summary>Ubicacion de los XSD oficiales de la AEAT.</summary>
public sealed class FiscalSchemaOptions
{
    public const string SectionName = "Fiscal";

    /// <summary>Carpeta que contiene los XSD oficiales.</summary>
    public string SchemaDirectory { get; set; } = "docs/verifactu/xsd";
}