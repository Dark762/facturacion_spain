namespace Facturacion.Application.Billing;

/// <summary>Fallo operativo de entrega; nunca contiene destinatario, código ni credenciales.</summary>
public sealed class EmailDeliveryException : Exception
{
    public string ErrorCode { get; }
    public bool IsTransient { get; }

    public EmailDeliveryException(string errorCode, bool isTransient, Exception? innerException = null)
        : base("No se pudo entregar el correo electrónico.", innerException)
    {
        ErrorCode = errorCode;
        IsTransient = isTransient;
    }
}
