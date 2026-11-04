namespace Facturacion.Application.Common.Results;

/// <summary>Error de negocio con código estable y estado HTTP previsto.</summary>
public sealed class BusinessException : Exception
{
    public string Code { get; }
    public int StatusCode { get; }

    public BusinessException(string code, string message, int statusCode = 400)
        : base(message)
    {
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("El código es obligatorio.", nameof(code));
        if (statusCode < 400 || statusCode > 599) throw new ArgumentOutOfRangeException(nameof(statusCode));
        Code = code;
        StatusCode = statusCode;
    }
}
