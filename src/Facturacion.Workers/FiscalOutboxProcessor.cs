using System.Text.Json;
using Facturacion.Application.Interfaces;
using Facturacion.Domain.Entities;
using Facturacion.Infrastructure.Persistence;
using Facturacion.Infrastructure.Persistence.Repositories;
using Facturacion.Verifactu.Xml;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Facturacion.Workers;

/// <summary>
/// Procesa los mensajes de la cola outbox de tipo fiscal.
///
/// Hace el trabajo que si se puede hacer sin red: construir el XML del
/// registro y validarlo contra los XSD oficiales de la AEAT. Si el documento
/// no valida, el mensaje se marca fallido con el motivo del esquema, que es
/// informacion accionable.
///
/// Deliberadamente NO envia nada a la AEAT: el transporte sigue sin
/// implementar. Un envio sin confirmar la via seria peor que no enviar.
/// </summary>
public sealed class FiscalOutboxProcessor
{
    public const string FiscalRecordCreated = "fiscal.record.created";

    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<FiscalOutboxProcessor> _logger;
    private readonly OutboxOptions _options;

    public FiscalOutboxProcessor(
        IServiceScopeFactory scopes,
        ILogger<FiscalOutboxProcessor> logger,
        OutboxOptions options)
    {
        _scopes = scopes;
        _logger = logger;
        _options = options;
    }

    /// <summary>Reclama mensajes de todos los tenants y los procesa.</summary>
    public async Task<int> ProcessBatchAsync(CancellationToken ct)
    {
        // El ambito es solo para reclamar. El trabajo real se hace en otro
        // ambito por tenant, porque TenantContext solo admite fijarse una vez
        // y un worker recorre empresas de clientes distintos.
        IReadOnlyList<OutboxMessage> claimed;
        await using (var claimScope = _scopes.CreateAsyncScope())
        {
            var repository = claimScope.ServiceProvider.GetRequiredService<IOutboxMessageRepository>();
            claimed = await repository.ClaimPendingAsync(_options.BatchSize, _options.Lease, ct);
        }

        if (claimed.Count == 0)
        {
            return 0;
        }

        var processed = 0;

        foreach (var group in claimed.GroupBy(x => x.TenantId))
        {
            foreach (var message in group)
            {
                await ProcessOneAsync(message, ct);
                processed++;
            }
        }

        return processed;
    }

    private async Task ProcessOneAsync(OutboxMessage message, CancellationToken ct)
    {
        if (!string.Equals(message.Type, FiscalRecordCreated, StringComparison.Ordinal))
        {
            // Tipos ajenos a este procesador (B2B, invitaciones). Se completan
            // para no quedar reintentando un mensaje que nadie va a atender.
            await CompleteAsync(message.Id, ct);
            return;
        }

        try
        {
            await using var scope = _scopes.CreateAsyncScope();

            var context = scope.ServiceProvider.GetRequiredService<BillingDbContext>();
            var builder = scope.ServiceProvider.GetRequiredService<IRecordXmlBuilder>();
            var validator = scope.ServiceProvider.GetRequiredService<IXsdValidator>();

            var recordId = ReadFiscalRecordId(message.Payload);

            var record = await context.FiscalRecords
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == recordId, ct);

            if (record is null)
            {
                // El registro no existe: el mensaje no se puede procesar ni
                // tiene sentido reintentarlo.
                await FailAsync(message.Id, ct,
                    $"El registro fiscal {recordId} no existe.", permanent: true);
                return;
            }

            if (string.IsNullOrWhiteSpace(record.XmlPayload))
            {
                await FailAsync(message.Id, ct,
                    "El registro fiscal no tiene XML: la emision no lo ha generado.", permanent: false);
                return;
            }

            var validation = validator.Validate(record.XmlPayload);

            if (!validation.IsValid)
            {
                // Un XML invalido no se arregla reintentando.
                _logger.LogError(
                    "XML invalido para el registro {RecordId}: {Errors}",
                    recordId, validation.Summary);

                await FailAsync(message.Id, ct, validation.Summary, permanent: true);
                return;
            }

            await CompleteAsync(message.Id, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Fallo al procesar el mensaje {MessageId}", message.Id);
            await FailAsync(message.Id, ct, ex.Message, permanent: false);
        }
    }

    private async Task CompleteAsync(Guid id, CancellationToken ct)
    {
        await using var scope = _scopes.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IOutboxMessageRepository>();
        await repository.MarkProcessedAsync(id, DateTimeOffset.UtcNow, ct);
    }

    private async Task FailAsync(Guid id, CancellationToken ct, string error, bool permanent)
    {
        await using var scope = _scopes.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IOutboxMessageRepository>();

        // Un fallo permanente se agota al instante: reintentar un documento
        // invalido unicamente retrasa la evidencia del problema.
        var maxAttempts = permanent ? 1 : _options.MaxAttempts;

        await repository.MarkFailedAsync(
            id, DateTimeOffset.UtcNow, error, maxAttempts, _options.BaseDelay, ct);
    }

    private static Guid ReadFiscalRecordId(string payload)
    {
        try
        {
            using var document = JsonDocument.Parse(payload);
            return document.RootElement.GetProperty("FiscalRecordId").GetGuid();
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException)
        {
            throw new InvalidOperationException($"Payload del outbox ilegible: {payload}", ex);
        }
    }
}

/// <summary>Configuracion del procesador de outbox.</summary>
public sealed class OutboxOptions
{
    public const string SectionName = "Outbox";

    /// <summary>Cuantos mensajes se reclaman por vuelta.</summary>
    public int BatchSize { get; set; } = 25;

    /// <summary>Espera entre vueltas.</summary>
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Tiempo que un mensaje permanece reclamado. Si el worker muere a mitad,
    /// el mensaje vuelve a la cola al expirar la lease.
    /// </summary>
    public TimeSpan Lease { get; set; } = TimeSpan.FromMinutes(2);

    /// <summary>Intentos antes de marcar el mensaje como fallido.</summary>
    public int MaxAttempts { get; set; } = 5;

    /// <summary>Espera base del reintento; luego se duplica en cada intento.</summary>
    public TimeSpan BaseDelay { get; set; } = TimeSpan.FromSeconds(30);
}