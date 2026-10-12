using Facturacion.Application.Common.Errors;
using Facturacion.Application.Common.Results;
using Facturacion.Application.DTOs.B2B;
using Facturacion.Application.Interfaces;
using Facturacion.Domain.Entities;

namespace Facturacion.Application.Services;

/// <summary>
/// Encola un documento electrónico ya generado. No genera Facturae/UBL
/// ni comunica directamente con una plataforma externa.
/// </summary>
public sealed class B2BInvoiceService : IB2BInvoiceService
{
    private readonly IInvoiceRepository _invoices;
    private readonly IElectronicDocumentRepository _documents;
    private readonly IOutboxMessageRepository _outbox;
    private readonly ICompanyAuthorizationService _authorization;
    private readonly ITenantContext _tenant;
    private readonly IUnitOfWork _unitOfWork;

    public B2BInvoiceService(IInvoiceRepository invoices, IElectronicDocumentRepository documents,
        IOutboxMessageRepository outbox, ICompanyAuthorizationService authorization,
        ITenantContext tenant, IUnitOfWork unitOfWork)
    {
        _invoices = invoices;
        _documents = documents;
        _outbox = outbox;
        _authorization = authorization;
        _tenant = tenant;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<B2BSubmissionResult>> SendAsync(Guid invoiceId, CancellationToken ct)
    {
        if (invoiceId == Guid.Empty)
            return Result<B2BSubmissionResult>.Failure(ApplicationErrors.Invalid(
                "INVOICE_ID_REQUIRED", "El identificador es obligatorio.", "invoiceId"));

        var invoice = await _invoices.GetByIdAsync(invoiceId, ct);
        if (invoice is null || invoice.TenantId != _tenant.TenantId)
            return Result<B2BSubmissionResult>.Failure(ApplicationErrors.NotFound("Factura"));

        var auth = await _authorization.AuthorizeAsync(invoice.CompanyId, "invoices.issue", ct);
        if (auth.IsFailure) return Result<B2BSubmissionResult>.Failure(auth.Errors.ToArray());

        if (invoice.Status != Facturacion.Domain.Enums.InvoiceStatus.Issued)
            return Result<B2BSubmissionResult>.Failure(new Error("INVOICE_NOT_ISSUED",
                "La factura debe estar emitida antes de transmitirla.", ErrorType.BusinessRule));

        // El repositorio debe consultar por InvoiceId dentro del tenant actual.
        var document = await _documents.GetByInvoiceAsync(invoiceId, ct);
        if (document is null || document.TenantId != _tenant.TenantId ||
            document.CompanyId != invoice.CompanyId || string.IsNullOrWhiteSpace(document.XmlPayload))
            return Result<B2BSubmissionResult>.Failure(new Error("B2B_DOCUMENT_MISSING",
                "No existe un documento electrónico preparado para esta factura.", ErrorType.BusinessRule));

        await _outbox.AddAsync(new OutboxMessage
        {
            TenantId = _tenant.TenantId,
            Type = "B2B_SEND_DOCUMENT",
            AggregateId = document.Id.ToString("D"),
            Payload = System.Text.Json.JsonSerializer.Serialize(new { DocumentId = document.Id }),
            Status = "Pending",
            CreatedAt = DateTimeOffset.UtcNow
        }, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        // Accepted=false porque aún no existe confirmación del proveedor.
        return Result<B2BSubmissionResult>.Success(new B2BSubmissionResult(
            false, null, "QUEUED", "Documento encolado; envío pendiente del worker."));
    }
}
