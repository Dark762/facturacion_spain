namespace Facturacion.Verifactu.Xml;

/// <summary>Validacion de un XML fiscal contra los XSD oficiales de la AEAT.</summary>
public interface IXsdValidator
{
    bool IsAvailable { get; }

    IReadOnlyList<string> Validate(string xml);
}

public sealed class XsdValidationUnavailableException()
    : InvalidOperationException(
        "La validacion XSD no esta implementada. Requiere los esquemas oficiales de la AEAT, que no " +
        "se han descargado ni contrastado en esta entrega. Un XML no validado contra el esquema es " +
        "justo el error que el envio remoto rechaza.")
{
}

/// <summary>Implementacion unica: falla siempre.</summary>
public sealed class UnavailableXsdValidator : IXsdValidator
{
    public bool IsAvailable => false;

    public IReadOnlyList<string> Validate(string xml) => throw new XsdValidationUnavailableException();
}