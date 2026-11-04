using Facturacion.Application.Common.Results;
using Microsoft.AspNetCore.Mvc;

namespace Facturacion.Api.Responses;

/// <summary>Respuestas uniformes conservando el código HTTP real.</summary>
public static class ApiResponseExtensions
{
    public static IActionResult ApiOk<T>(this ControllerBase controller, T data,
        string message = "Operación realizada correctamente.", string code = "OK") =>
        controller.Ok(ApiResponse<T>.Ok(data, message, code, controller.HttpContext.TraceIdentifier));

    public static IActionResult ApiCreated<T>(this ControllerBase controller, string location, T data,
        string message = "Recurso creado correctamente.", string code = "CREATED") =>
        controller.Created(location, ApiResponse<T>.Ok(data, message, code, controller.HttpContext.TraceIdentifier));

    public static IActionResult ApiFailure<T>(this ControllerBase controller, int statusCode,
        string code, string message, IEnumerable<Error>? errors = null) =>
        controller.StatusCode(statusCode,
            ApiResponse<T>.Fail(code, message, errors, controller.HttpContext.TraceIdentifier));
}
