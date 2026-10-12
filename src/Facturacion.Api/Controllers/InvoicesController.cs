using Facturacion.Application.Common.Results;
using Facturacion.Application.DTOs.Invoices;
using Facturacion.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Facturacion.Api.Controllers;

/// <summary>
/// El TenantId se toma siempre del JWT resuelto por TenantResolutionMiddleware;
/// nunca del cuerpo de la peticion.
/// </summary>
[ApiController]
[Authorize]
[Route("api/invoices")]
public sealed class InvoicesController : ControllerBase
{
    private readonly IInvoiceApplicationService _invoices;
    private readonly IInvoiceIssuanceService _issuance;

    public InvoicesController(
        IInvoiceApplicationService invoices, IInvoiceIssuanceService issuance)
    {
        _invoices = invoices;
        _issuance = issuance;
    }

    public sealed record IssueRequest(string Series);

    [HttpPost]
    public async Task<IActionResult> CreateDraft(
        [FromBody] CreateInvoiceRequest request, CancellationToken ct) =>
        ToAction(await _invoices.CreateDraftAsync(request, ct));

    [HttpGet("{invoiceId:guid}")]
    public async Task<IActionResult> Get(Guid invoiceId, CancellationToken ct) =>
        ToAction(await _invoices.GetByIdAsync(invoiceId, ct));

    /// <summary>
    /// Emite la factura: asigna la numeracion definitiva dentro de una transaccion
    /// y la marca como emitida. Requiere el permiso invoices.issue (rol Admin).
    /// </summary>
    [HttpPost("{invoiceId:guid}/issue")]
    public async Task<IActionResult> Issue(
        Guid invoiceId, [FromBody] IssueRequest request, CancellationToken ct) =>
        ToAction(await _issuance.IssueAsync(invoiceId, request.Series, ct));

    private static IActionResult ToAction<T>(Result<T> result)
    {
        if (result.IsSuccess) return new OkObjectResult(result.Value);

        var first = result.Errors[0];
        return first.Type switch
        {
            ErrorType.Unauthorized => new UnauthorizedObjectResult(first),
            ErrorType.Forbidden => new StatusCodeResult(StatusCodes.Status403Forbidden),
            ErrorType.NotFound => new NotFoundObjectResult(first),
            ErrorType.Conflict => new ConflictObjectResult(first),
            _ => new BadRequestObjectResult(first)
        };
    }
}