using Facturacion.Application.Common.Results;
using Facturacion.Api.Responses;
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

    private IActionResult ToAction<T>(Result<T> result)
    {
        if (result.IsSuccess) return this.ApiOk(result.Value);

        var first = result.Errors.FirstOrDefault();
        if (first is null)
            return this.ApiFailure<object>(500, "UNKNOWN_ERROR", "No se pudo completar la operación.");
        var status = first.Type switch
        {
            ErrorType.Unauthorized => 401,
            ErrorType.Forbidden => 403,
            ErrorType.NotFound => 404,
            ErrorType.Conflict => 409,
            ErrorType.ExternalService => 503,
            ErrorType.Internal => 500,
            _ => 400
        };
        return this.ApiFailure<object>(status, first.Code, first.Message, result.Errors);
    }
}