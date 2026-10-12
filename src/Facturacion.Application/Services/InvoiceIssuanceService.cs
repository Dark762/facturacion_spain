using Facturacion.Application.Common.Errors;
using Facturacion.Application.Common.Results;
using Facturacion.Application.DTOs.Invoices;
using Facturacion.Application.Interfaces;
using Facturacion.Domain.Entities;
using Facturacion.Domain.Enums;
using Facturacion.Verifactu;

namespace Facturacion.Application.Services;

/// <summary>
/// Emision de facturas con numeracion definitiva.
/// Todo el proceso ocurre en una sola transaccion sobre BillingDbContext:
/// validar motor fiscal -> bloquear secuencia -> asignar numero -> generar
/// registro fiscal -> encadenar -> encolar en Outbox -> confirmar.
/// Si algo falla, no queda ni la secuencia advanced ni una factura a medio emitir.
/// </summary>
public sealed class InvoiceIssuanceService(
    IInvoiceRepository invoices,
    IInvoiceSequenceRepository sequences,
    IUnitOfWork unitOfWork,
    ICompanyAuthorizationService authorization,
    IOutboxMessageRepository outbox,
    IFiscalRecordRepository fiscalRecords,
    IFiscalChainRepository fiscalChains,
    IFiscalEngine fiscalEngine,
    ITenantContext tenant) : IInvoiceIssuanceService
{
    private const int MaxAttempts = 4;

    private const string OutboxType = "fiscal.record.created";
    private const string OutboxStatusPending = "Pending";

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
        // a tocar la secuencia. El registro fiscal debe EXISTIR: devolver
        // Guid.Empty obligaba al consumidor a tratar un identificador ficticio
        // como si fuera real.
        if (invoice.Status != InvoiceStatus.Draft)
        {
            if (invoice.IssuedAt is not null && invoice.Number is not null)
                return await ResolveAlreadyIssuedAsync(invoice, ct);

            return Fail("INVOICE_NOT_DRAFT",
                $"Solo se pueden emitir facturas en borrador (estado actual: {invoice.Status}).", "status");
        }

        if (invoice.Lines.Count == 0)
            return Fail("INVOICE_HAS_NO_LINES", "La factura no tiene lineas.", "lines");

        // Puerta fail-closed ANTES de consumir numeracion. Se comprueba aqui y
        // no dentro de la transaccion a proposito: si el motor fiscal no esta
        // completo, la emision se rechaza sin Advanced la secuencia, sin crear
        // la fila de secuencia y sin dejar la factura a medias.
        if (!fiscalEngine.IsReadyForIssuance)
        {
            return Result<InvoiceIssueResult>.Failure(ApplicationErrors.Invalid(
                "FISCAL_ENGINE_NOT_CONFIGURED",
                "El motor fiscal no esta habilitado, por lo que no se puede emitir. " +
                "Componentes pendientes: " + string.Join(", ", fiscalEngine.Blockers) + ".",
                "fiscalEngine"));
        }

        // La identificacion del sistema forma parte del contenido del registro
        // fiscal. Si le falta un dato obligatorio, tampoco se emite: un nombre
        // de software o un numero de instalacion ficticios acabarian en un
        // documento dirigido a Hacienda.
        if (!FiscalEngineSoftware.IsComplete)
        {
            return Result<InvoiceIssueResult>.Failure(ApplicationErrors.Invalid(
                "SYSTEM_IDENTIFICATION_NOT_CONFIGURED",
                FiscalEngineSoftware.Describe(),
                "systemIdentification"));
        }

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

// Tras un reintento la factura puede haberse emitido en otro intento
                    // concurrente: se resuelve su registro fiscal real.
                    if (invoice.Status != InvoiceStatus.Draft)
                        return await ResolveAlreadyIssuedAsync(invoice, ct);
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

// Una SOLA marca temporal para toda la emision. Antes se usaba
            // DateTimeOffset.UtcNow para IssuedAt y GeneratedAt, y
            // DateTimeOffset.Now para la huella: dos instantes distintos con
            // desplazamientos distintos. La huella es el dato que la AEAT
            // verifica, asi que debe salir de la MISMA lectura que el resto.
            var issuedAt = DateTimeOffset.Now;

            // Serie, numero e IssuedAt se asignan juntos: la restriccion
            // CK_Invoices_IssuedNumber exige que IssuedAt no se informe sin
            // serie y numero.
            invoice.Series = series;
            invoice.Number = number.ToString();
            invoice.Status = InvoiceStatus.Issued;
            invoice.IssuedAt = issuedAt;

            await unitOfWork.SaveChangesAsync(ct);

            // El registro fiscal y el mensaje de Outbox se crean DENTRO de la
            // misma transaccion que la factura y la secuencia. Si el commit
            // falla, no queda ni factura emitida, ni numero consumido, ni evento
            // pendiente: el Outbox nunca puede quedar huerfano de su factura.
            var fiscalRecordId = await CreateFiscalRecordAsync(invoice, issuedAt, ct);
            await EnqueueOutboxAsync(invoice, fiscalRecordId, issuedAt, ct);

            await unitOfWork.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            return Result<InvoiceIssueResult>.Success(new InvoiceIssueResult(
                invoice.Id, $"{series}/{number}", fiscalRecordId));
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

/// <summary>
    /// Resuelve el resultado de una factura ya emitida, localizando su registro
    /// fiscal REAL en lugar de devolver un identificador ficticio.
    ///
    /// Si el registro no existe o pertenece a otro tenant, es una
    /// inconsistencia: se rechaza con un error explicito en vez de propagar
    /// un Guid.Empty que el consumidor no podria distinguir de un identificador
    /// valido.
    /// </summary>
    private async Task<Result<InvoiceIssueResult>> ResolveAlreadyIssuedAsync(
        Invoice invoice, CancellationToken ct)
    {
        var records = await fiscalRecords.GetByInvoiceAsync(invoice.Id, ct);
        var record = records.FirstOrDefault(r => r.RecordType == FiscalRecordType.Alta);

        if (record is null)
            return Fail("FISCAL_RECORD_MISSING",
                $"La factura {invoice.Id} figura emitida pero no tiene registro fiscal de alta. " +
                "No se devuelve un identificador ficticio: la cadena fiscal quedaria huerfana.",
                "fiscalRecord");

        if (record.TenantId != invoice.TenantId)
            return Fail("FISCAL_RECORD_TENANT_MISMATCH",
                "El registro fiscal pertenece a otro tenant. Operacion rechazada.",
                "fiscalRecord");

        return Result<InvoiceIssueResult>.Success(new InvoiceIssueResult(
            invoice.Id, $"{invoice.Series}/{invoice.Number}", record.Id));
    }

    /// <summary>
    /// Numero de factura segun el XSD oficial: NumSerieFactura y Numero van en
    /// el MISMO campo separados por "/". Es el unico separador que admite el
    /// tipo TextoIDFacturaType, y el que usan los vectores oficiales.
    /// </summary>
    private static string BuildInvoiceNumber(Invoice invoice)
    {
        var series = invoice.Series?.Trim();
        var number = invoice.Number?.Trim();

        if (string.IsNullOrEmpty(series)) return number ?? string.Empty;
        if (string.IsNullOrEmpty(number)) return series;

        return $"{series}/{number}";
    }

    /// <summary>
    /// Genera el registro fiscal de alta. Lanza si el motor no puede calcular
    /// la huella: es preferible abortar la emision que persistir un registro
    /// fiscal sin huella, que seria indistinguible de uno real.
    /// </summary>
    private async Task<Guid> CreateFiscalRecordAsync(Invoice invoice, DateTimeOffset issuedAt, CancellationToken ct)
    {
        var recordId = Guid.NewGuid();
        var company = await invoices.GetCompanyAsync(invoice.CompanyId, ct);
        if (company is null)
            throw new InvalidOperationException(
                $"No se encuentra la empresa emisora {invoice.CompanyId} de la factura {invoice.Id}.");

        var chain = await fiscalChains.GetByCompanyAsync(invoice.TenantId, invoice.CompanyId, ct);
        var previousHash = chain?.LastHash ?? string.Empty;

        var data = new Verifactu.Validation.FiscalData(
            company.Nif,
            invoice.Number ?? string.Empty,
            invoice.Series,
            invoice.IssueDate,
            invoice.Type.ToString(),
            invoice.TaxableBase,
            invoice.TaxAmount,
            invoice.Total,
            invoice.Lines
                .Select(l => new Verifactu.Validation.FiscalLineData(
                    l.Description, l.Quantity, l.UnitPrice, l.VatRate, l.TotalAmount))
                .ToArray());

        var errors = fiscalEngine.Verifier.Verify(data);
        if (errors.Count > 0)
            throw new InvalidOperationException(
                "La factura no supera la validacion fiscal: " +
                string.Join("; ", errors.Select(e => $"{e.Code} ({e.Field})")));

// Formatos exigidos por la especificacion oficial de la AEAT v0.1.2:
        //   FechaExpedicionFactura -> dd-MM-yyyy
        //   FechaHoraHusoGenRegistro -> ISO-8601 con desplazamiento horario
        //   Importes -> punto decimal, sin miles, invariante de cultura
        var fingerprint = fiscalEngine.Fingerprint.Compute(
            new Verifactu.Canonicalization.CanonicalizationRequest(
                Verifactu.Canonicalization.FiscalRecordKind.Alta,
                company.Nif,
                BuildInvoiceNumber(invoice),
                invoice.IssueDate.ToString("dd-MM-yyyy", System.Globalization.CultureInfo.InvariantCulture),
                invoice.Type.ToString(),
                invoice.TaxAmount.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                invoice.Total.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                string.IsNullOrEmpty(previousHash) ? null : previousHash,
                issuedAt.ToString("yyyy-MM-ddTHH:mm:sszzz", System.Globalization.CultureInfo.InvariantCulture)));

        var chainProblem = fiscalEngine.Chain.VerifyChaining(
            string.IsNullOrEmpty(previousHash) ? null : previousHash,
            previousHash,
            fingerprint.Hash);
        if (chainProblem is not null)
            throw new InvalidOperationException($"Cadena fiscal incoherente: {chainProblem}");

        // La cadena DEBE avanzar. Sin esta actualizacion cada registro se
        // encadenaria como si fuera el primero y la trazabilidad se perderia,
        // aunque la huella se calculase bien.
        var head = fiscalEngine.Chain.BuildNextHead(
            new Verifactu.Chain.ChainHead(chain?.Id, chain?.LastHash ?? string.Empty),
            recordId,
            fingerprint.Hash);

        if (chain is null)
        {
            chain = new FiscalChain
            {
                TenantId = invoice.TenantId,
                CompanyId = invoice.CompanyId,
                InstallationId = FiscalEngineSoftware.Current.InstallationNumber,
                LastRecordId = head.LastRecordId,
                LastHash = head.LastHash,
                UpdatedAt = issuedAt
            };
            await fiscalChains.AddAsync(chain, ct);
        }
        else
        {
            chain.LastRecordId = head.LastRecordId;
            chain.LastHash = head.LastHash;
            chain.UpdatedAt = issuedAt;
        }

        var record = new FiscalRecord
        {
            Id = recordId,
            TenantId = invoice.TenantId,
            InvoiceId = invoice.Id,
            RecordType = FiscalRecordType.Alta,
            PreviousHash = fingerprint.PreviousHash,
            Hash = fingerprint.Hash,
            GeneratedAt = issuedAt,
            SoftwareName = FiscalEngineSoftware.Current.SystemName,
            SoftwareVersion = FiscalEngineSoftware.Current.SystemVersion,
            InstallationId = FiscalEngineSoftware.Current.InstallationNumber,
            XmlPayload = string.Empty,
            SubmissionStatus = SubmissionStatus.Pending
        };

        await fiscalRecords.AddAsync(record, ct);
        return record.Id;
    }

    /// <summary>
    /// Encola el evento de forma atomica. El mensaje no lo consume ningun
    /// worker todavia: la Fase 4 (adaptadores AEAT) esta diferida.
    /// </summary>
    private async Task EnqueueOutboxAsync(Invoice invoice, Guid fiscalRecordId, DateTimeOffset issuedAt, CancellationToken ct)
    {
        var message = new OutboxMessage
        {
            TenantId = invoice.TenantId,
            Type = OutboxType,
            AggregateId = fiscalRecordId.ToString(),
            Payload = $"{{\"InvoiceId\":\"{invoice.Id}\",\"FiscalRecordId\":\"{fiscalRecordId}\"," +
                      $"\"InvoiceNumber\":\"{invoice.Series}/{invoice.Number}\"}}",
            Status = OutboxStatusPending,
            CreatedAt = issuedAt,
            NextAttemptAt = issuedAt
        };

        await outbox.AddAsync(message, ct);
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