namespace Facturacion.Verifactu.Qr;

/// <summary>
/// Genera el contenido del codigo QR que se imprime en la factura.
/// La FAQ oficial confirma que la huella del registro NO forma parte del QR.
/// El resto de parametros no estan verificados en esta entrega.
/// </summary>
public interface IQrGenerator
{
    bool IsAvailable { get; }

    string BuildVerificationUrl(string issuerNif, string invoiceNumber, decimal total);
}

public sealed class QrUnavailableException()
    : InvalidOperationException(
        "La generacion del QR no esta implementada. Los parametros y el formato de la URL de cotejo " +
        "no se han verificado contra la especificacion oficial en esta entrega. La FAQ oficial solo " +
        "permite afirmar que la huella del registro no forma parte del QR.")
{
}

/// <summary>Implementacion unica: falla siempre.</summary>
public sealed class UnavailableQrGenerator : IQrGenerator
{
    public bool IsAvailable => false;

    public string BuildVerificationUrl(string issuerNif, string invoiceNumber, decimal total) =>
        throw new QrUnavailableException();
}