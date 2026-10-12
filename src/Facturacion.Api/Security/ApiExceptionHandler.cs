using System.Net;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Facturacion.Api.Security;

/// <summary>
/// Traduce las excepciones de la aplicacion a codigos HTTP correctos. Sin esto
/// un error de validacion de dominio sale como 500 y el cliente no puede
/// distinguir "entrada invalida" de "fallo del servidor".
/// </summary>
public sealed class ApiExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService _problems;

    public ApiExceptionHandler(IProblemDetailsService problems) => _problems = problems;

    public async ValueTask<bool> TryHandleAsync(
        HttpContext context, Exception exception, CancellationToken ct)
    {
        var (status, title) = exception switch
        {
            UnauthorizedAccessException => ((int)HttpStatusCode.Forbidden, "Acceso denegado"),
            KeyNotFoundException => ((int)HttpStatusCode.NotFound, "Recurso no encontrado"),
            ArgumentException => ((int)HttpStatusCode.BadRequest, "Solicitud invalida"),
            InvalidOperationException => ((int)HttpStatusCode.Conflict, "Operacion no permitida"),
            _ => ((int)HttpStatusCode.InternalServerError, "Error interno")
        };

        // El detalle solo se expone fuera de produccion: en produccion un 500
        // no debe revelar estructura interna.
        var detail = context.RequestServices
            .GetRequiredService<IHostEnvironment>().IsDevelopment()
            ? exception.Message
            : null;

        context.Response.StatusCode = status;
        return await _problems.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = status,
                Title = title,
                Detail = detail,
                Instance = context.Request.Path
            }
        });
    }
}