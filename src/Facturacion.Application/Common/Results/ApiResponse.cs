namespace Facturacion.Application.Common.Results;
public sealed record ApiResponse<T>(bool Success, T? Data, IReadOnlyList<Error> Errors, string? TraceId = null);
