using Facturacion.Application.Common.Errors;
using Facturacion.Application.Common.Results;
using Facturacion.Application.DTOs.Fiscal;
using Facturacion.Application.Interfaces;
using Facturacion.Domain.Entities;
using Facturacion.Domain.Enums;

namespace Facturacion.Application.Services;

/// <summary>
/// Encola el registro fiscal para procesamiento asíncrono. NO lo envía a AEAT.
/// Un worker debe reclamar el Outbox de manera atómica y efectuar el envío real.
/// </summary>
public sealed class AeatSubmissionService : IAeatSubmissionService
{
    private readonly IFiscalRecordRepository _records;
    private readonly IInvoiceRepository _invoices;
    private readonly ICompanyAuthorizationService _authorization;
    private readonly IOutboxMessageRepository _outbox;
    private readonly ITenantContext _tenant;
    private readonly IUnitOfWork _unitOfWork;

    public AeatSubmissionService(IFiscalRecordRepository records, IInvoiceRepository invoices,
        ICompanyAuthorizationService authorization, IOutboxMessageRepository outbox,
        ITenantContext tenant, IUnitOfWork unitOfWork)
    {
        _records = records;
        _invoices = invoices;
        _authorization = authorization;
        _outbox = outbox;
        _tenant = tenant;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<AeatSubmissionResult>> SubmitAsync(Guid fiscalRecordId, CancellationToken ct)
    {
        if (fiscalRecordId == Guid.Empty)
            return Result<AeatSubmissionResult>.Failure(ApplicationErrors.Invalid(
                "FISCAL_RECORD_ID_REQUIRED", "El identificador es obligatorio.", "fiscalRecordId"));

        var record = await _records.GetByIdAsync(fiscalRecordId, ct);
        if (record is null || record.TenantId != _tenant.TenantId)
            return Result<AeatSubmissionResult>.Failure(ApplicationErrors.NotFound("Registro fiscal"));

        var invoice = await _invoices.GetByIdAsync(record.InvoiceId, ct);
        if (invoice is null || invoice.TenantId != _tenant.TenantId)
            return Result<AeatSubmissionResult>.Failure(ApplicationErrors.NotFound("Factura"));

        var auth = await _authorization.AuthorizeAsync(invoice.CompanyId, "invoices.issue", ct);
        if (auth.IsFailure) return Result<AeatSubmissionResult>.Failure(auth.Errors.ToArray());

        if (string.IsNullOrWhiteSpace(record.XmlPayload) || string.IsNullOrWhiteSpace(record.Hash))
            return Result<AeatSubmissionResult>.Failure(new Error("FISCAL_RECORD_INCOMPLETE",
                "El registro fiscal no contiene XML y huella; no puede enviarse.", ErrorType.BusinessRule));

        if (record.SubmissionStatus != SubmissionStatus.Pending)
            return Result<AeatSubmissionResult>.Failure(new Error("FISCAL_RECORD_NOT_PENDING",
                "El registro fiscal no está pendiente. Evite encolados duplicados.", ErrorType.Conflict));

        // El estado Processing significa encolado, NO presentado ni aceptado por AEAT.
        record.SubmissionStatus = SubmissionStatus.Processing;
        await _outbox.AddAsync(new OutboxMessage
        {
            TenantId = _tenant.TenantId,
            Type = "AEAT_SUBMIT_FISCAL_RECORD",
            AggregateId = record.Id.ToString("D"),
            Payload = System.Text.Json.JsonSerializer.Serialize(new { FiscalRecordId = record.Id }),
            CreatedAt = DateTimeOffset.UtcNow,
            Status = "Pending"
        }, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<AeatSubmissionResult>.Success(new AeatSubmissionResult(record.Id,
            "Queued", null, null));
    }
}
