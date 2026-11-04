namespace Facturacion.Application.Common.Results;

/// <summary>Contrato uniforme de respuestas HTTP. Mantiene el constructor original para compatibilidad.</summary>
public sealed record ApiResponse<T>(bool Success, T? Data, IReadOnlyList<Error> Errors, string? TraceId = null)
{
    public string Code { get; init; } = Success ? "OK" : "ERROR";
    public string Message { get; init; } = Success ? "Operación realizada correctamente." : "No se pudo completar la operación.";

    public static ApiResponse<T> Ok(T data, string message = "Operación realizada correctamente.",
        string code = "OK", string? traceId = null) =>
        new(true, data, Array.Empty<Error>(), traceId) { Code = code, Message = message };

    public static ApiResponse<T> Fail(string code, string message, IEnumerable<Error>? errors = null,
        string? traceId = null) =>
        new(false, default, errors?.ToArray() ?? Array.Empty<Error>(), traceId)
        { Code = code, Message = message };
}
