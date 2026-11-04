using System.Net;
using Facturacion.Application.Common.Results;
using Microsoft.AspNetCore.Diagnostics;

namespace Facturacion.Api.Security;

/// <summary>Devuelve el contrato estándar sin revelar detalles internos de errores 500.</summary>
public sealed class ApiExceptionHandler : IExceptionHandler
{
    private readonly ILogger<ApiExceptionHandler> _logger;

    public ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) => _logger = logger;

    public async ValueTask<bool> TryHandleAsync(
        HttpContext context, Exception exception, CancellationToken ct)
    {
        var (status, code, message) = exception switch
        {
            BusinessException business => (business.StatusCode, business.Code, business.Message),
            UnauthorizedAccessException => (403, "FORBIDDEN", "No tienes permisos para esta operación."),
            KeyNotFoundException => (404, "NOT_FOUND", "Recurso no encontrado."),
            ArgumentException => (400, "INVALID_REQUEST", "La solicitud no es válida."),
            InvalidOperationException => (409, "BUSINESS_CONFLICT", "La operación no se puede completar en el estado actual."),
            _ => (500, "INTERNAL_ERROR", "Se ha producido un error inesperado. Inténtalo de nuevo.")
        };

        _logger.LogError(exception, "Error HTTP {StatusCode}, código {ErrorCode}, trace {TraceId}",
            status, code, context.TraceIdentifier);

        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(
            ApiResponse<object>.Fail(code, message, traceId: context.TraceIdentifier),
            cancellationToken: ct);
        return true;
    }
}
