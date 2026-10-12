namespace Facturacion.Verifactu.Xml;

using Facturacion.Verifactu.Records;

/// <summary>
/// Serializa un registro fiscal al XML que se remite.
/// La estructura debe validar contra los XSD oficiales de la AEAT.
/// </summary>
public interface IRecordXmlBuilder
{
    bool IsAvailable { get; }

    string BuildXml(FiscalRecordDraft record);
}

public sealed class XmlBuilderUnavailableException()
    : InvalidOperationException(
        "La generacion de XML no esta implementada. La estructura debe validar contra los esquemas " +
        "XSD oficiales de la AEAT, que no han sido descargados ni contrastados en esta entrega. " +
        "No se genera un XML aproximado porque la remision seria rechazada.")
{
}

/// <summary>Implementacion unica: falla siempre. Ver <see cref="XmlBuilderUnavailableException"/>.</summary>
public sealed class UnavailableRecordXmlBuilder : IRecordXmlBuilder
{
    public bool IsAvailable => false;

    public string BuildXml(FiscalRecordDraft record) => throw new XmlBuilderUnavailableException();
}