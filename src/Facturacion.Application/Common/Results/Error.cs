namespace Facturacion.Application.Common.Results;
public sealed record Error(string Code, string Message, ErrorType Type, string? Field = null);
