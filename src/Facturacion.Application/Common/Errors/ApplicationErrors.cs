using Facturacion.Application.Common.Results;
namespace Facturacion.Application.Common.Errors;
public static class ApplicationErrors
{
    public static Error NotFound(string resource) => new("RESOURCE_NOT_FOUND", $"{resource} no encontrado.", ErrorType.NotFound);
    public static Error Forbidden() => new("ACCESS_DENIED", "No tiene permiso para realizar esta operación.", ErrorType.Forbidden);
    public static Error Invalid(string code, string message, string? field = null) => new(code, message, ErrorType.Validation, field);
}
