using Facturacion.Application.Common.Errors;
using Facturacion.Application.Common.Results;
using Facturacion.Application.DTOs.Invoices;
using Facturacion.Application.Interfaces;
using Facturacion.Domain.Entities;
using Facturacion.Domain.Enums;

namespace Facturacion.Application.Services;

/// <summary>
/// Emision de facturas con numeracion definitiva.
/// Todo el proceso ocurre en una sola transaccion sobre BillingDbContext:
/// bloquear secuencia -> asignar numero -> marcar emitida -> confirmar.
/// Si algo falla, no queda ni la secuencia advanced ni una factura a medio emitir.
/// </summary>
public sealed class InvoiceIssuanceService(
    IInvoiceRepository invoices,
    IInvoiceSequenceRepository sequences,
    IUnitOfWork unitOfWork,
    ICompanyAuthorizationService authorization,
    ITenantContext tenant) : IInvoiceIssuanceService
{
    private const int MaxAttempts = 4;

    public async Task<Result<InvoiceIssueResult>> IssueAsync(
        Guid invoiceId, string series, CancellationToken ct)
    {
        if (invoiceId == Guid.Empty)
            return Fail("INVOICE_ID_REQUIRED", "El identificador es obligatorio.", "invoiceId");

        var normalizedSeries = series?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedSeries))
            return Fail("SERIES_REQUIRED", "La serie es obligatoria.", "series");
        if (normalizedSeries.Length > 32)
            return Fail("SERIES_TOO_LONG", "La serie no puede superar 32 caracteres.", "series");

        var invoice = await invoices.GetByIdAsync(invoiceId, ct);
        if (invoice is null || invoice.TenantId != tenant.TenantId)
            return Result<InvoiceIssueResult>.Failure(ApplicationErrors.NotFound("Factura"));

        var access = await authorization.AuthorizeAsync(invoice.CompanyId, "invoices.issue", ct);
        if (access.IsFailure)
            return Result<InvoiceIssueResult>.Failure(access.Errors.ToArray());

        // Idempotencia: una factura ya emitida devuelve su numeracion sin volver
        // a tocar la secuencia.
        if (invoice.Status != InvoiceStatus.Draft)
        {
            if (invoice.IssuedAt is not null && invoice.Number is not null)
                return Result<InvoiceIssueResult>.Success(new InvoiceIssueResult(
                    invoice.Id, $"{invoice.Series}/{invoice.Number}", Guid.Empty));

            return Fail("INVOICE_NOT_DRAFT",
                $"Solo se pueden emitir facturas en borrador (estado actual: {invoice.Status}).", "status");
        }

        if (invoice.Lines.Count == 0)
            return Fail("INVOICE_HAS_NO_LINES", "La factura no tiene lineas.", "lines");

        // La primera emision de una serie crea su fila de secuencia. Varias
        // emisiones simultaneas pueden competir por esa creacion, lo que produce
        // deadlock o clave duplicada. Solo reintentar la transaccion completa
        // recupera el contexto de forma segura tras un rollback.
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                // Tras un rollback las entidades siguen marcadas como pendientes:
                // hay que recargar antes de volver a intentarlo.
                if (attempt > 1)
                {
                    await unitOfWork.ResetAsync(ct);
                    var reloaded = await invoices.GetByIdAsync(invoiceId, ct);
                    if (reloaded is null)
                        return Result<InvoiceIssueResult>.Failure(ApplicationErrors.NotFound("Factura"));
                    invoice = reloaded;

                    if (invoice.Status != InvoiceStatus.Draft)
                        return Result<InvoiceIssueResult>.Success(new InvoiceIssueResult(
                            invoice.Id, $"{invoice.Series}/{invoice.Number}", Guid.Empty));
                }

                return await IssueCoreAsync(invoice, normalizedSeries, ct);
            }
            catch (Exception ex) when (IsRetryable(ex) && attempt < MaxAttempts)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(25 * attempt), ct);
            }
        }
    }

    private async Task<Result<InvoiceIssueResult>> IssueCoreAsync(
        Invoice invoice, string series, CancellationToken ct)
    {
        var fiscalYear = invoice.IssueDate.Year;

        await using var tx = await unitOfWork.BeginTransactionAsync(ct);
        try
        {
            var sequence = await sequences.LockBySeriesAsync(
                invoice.CompanyId, series, fiscalYear, ct);

            if (sequence is null)
            {
                sequence = new InvoiceSequence
                {
                    TenantId = tenant.TenantId,
                    CompanyId = invoice.CompanyId,
                    Series = series,
                    FiscalYear = fiscalYear,
                    NextNumber = 1
                };
                await sequences.AddAsync(sequence, ct);
                await unitOfWork.SaveChangesAsync(ct);
            }

            var number = sequence.NextNumber;
            sequence.NextNumber = number + 1;

            // Serie, numero e IssuedAt se asignan juntos: la restriccion
            // CK_Invoices_IssuedNumber exige que IssuedAt no se informe sin
            // serie y numero.
            invoice.Series = series;
            invoice.Number = number.ToString();
            invoice.Status = InvoiceStatus.Issued;
            invoice.IssuedAt = DateTimeOffset.UtcNow;

            await unitOfWork.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            return Result<InvoiceIssueResult>.Success(new InvoiceIssueResult(
                invoice.Id, $"{series}/{number}", Guid.Empty));
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    /// <summary>
    /// Deadlock, lock wait timeout o clave duplicada al crear la secuencia son
    /// fallos transitorios: se resuelven reintentando la transaccion entera.
    /// Se compara por nombre de tipo y no por la clase concreta para no
    /// introducir una dependencia de EF Core o del proveedor en Application.
    /// </summary>
    private static bool IsRetryable(Exception ex)
    {
        for (Exception? current = ex; current is not null; current = current.InnerException)
        {
            var type = current.GetType().FullName ?? string.Empty;
            var message = current.Message;

            // MySQL / MariaDB: 1213 deadlock, 1205 lock wait timeout, 1062 duplicate
            if (type.Contains("MySql") &&
                (message.Contains("Deadlock", StringComparison.OrdinalIgnoreCase) ||
                 message.Contains("Lock wait timeout", StringComparison.OrdinalIgnoreCase) ||
                 message.Contains("Duplicate entry", StringComparison.OrdinalIgnoreCase)))
                return true;

            // Npgsql: deadlock, serialization failure, unique violation
            if (type.Contains("Npgsql") &&
                (message.Contains("deadlock", StringComparison.OrdinalIgnoreCase) ||
                 message.Contains("could not serialize", StringComparison.OrdinalIgnoreCase) ||
                 message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase)))
                return true;
        }

        return false;
    }

    private static Result<InvoiceIssueResult> Fail(string code, string message, string field) =>
        Result<InvoiceIssueResult>.Failure(ApplicationErrors.Invalid(code, message, field));
}