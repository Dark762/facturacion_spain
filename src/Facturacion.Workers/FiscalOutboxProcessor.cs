using System.Text.Json;
using Facturacion.Application.Interfaces;
using Facturacion.Domain.Entities;
using Facturacion.Infrastructure.Multitenancy;
using Facturacion.Infrastructure.Persistence;
using Facturacion.Infrastructure.Persistence.Repositories;
using Facturacion.Verifactu.Xml;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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

    /// <summary>Reclama mensajes de tipo fiscal de todos los tenants y los procesa con aislamiento estricto.</summary>
    public async Task<int> ProcessBatchAsync(CancellationToken ct)
    {
        // Reclama EXCLUSIVAMENTE mensajes fiscales: otros eventos (B2B, invitaciones)
        // quedan disponibles para sus propios procesadores.
        IReadOnlyList<OutboxMessage> claimed;
        await using (var claimScope = _scopes.CreateAsyncScope())
        {
            var repository = claimScope.ServiceProvider.GetRequiredService<IOutboxMessageRepository>();
            claimed = await repository.ClaimPendingAsync(_options.BatchSize, _options.Lease, FiscalRecordCreated, ct);
        }

        if (claimed.Count == 0)
        {
            return 0;
        }

        var processed = 0;

        foreach (var message in claimed)
        {
            try
            {
                await ProcessOneAsync(message, ct);
                processed++;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(
                    ex, "Fallo no controlado al procesar mensaje {MessageId} del tenant {TenantId}. Continuan los demas.",
                    message.Id, message.TenantId);
            }
        }

        return processed;
    }

    private async Task ProcessOneAsync(OutboxMessage message, CancellationToken ct)
    {
        // Seguridad: el procesador fiscal nunca reclama ni consume tipos ajenos.
        // NUNCA marcar como completado un evento que no se ha procesado.
        if (!string.Equals(message.Type, FiscalRecordCreated, StringComparison.Ordinal))
        {
            _logger.LogWarning(
                "El mensaje {MessageId} con tipo '{Type}' no corresponde a {ExpectedType}. Se ignora para no perderlo.",
                message.Id, message.Type, FiscalRecordCreated);
            return;
        }

        // Si el tenant es inexistente o Guid.Empty, se rechaza inmediatamente sin ejecutar consultas de negocio.
        if (message.TenantId == Guid.Empty)
        {
            _logger.LogError("El mensaje {MessageId} no tiene un TenantId valido.", message.Id);
            await FailDirectAsync(message, "El mensaje no tiene un TenantId valido (Guid.Empty).", permanent: true, ct);
            return;
        }

        try
        {
            // 1. Crear un nuevo scope DI por mensaje.
            await using var scope = _scopes.CreateAsyncScope();

            // 2 y 3. Resolver TenantContext y fijar TenantId ANTES de resolver o usar DbContext.
            SetScopeTenant(scope.ServiceProvider, message.TenantId);

            var context = scope.ServiceProvider.GetRequiredService<BillingDbContext>();
            var validator = scope.ServiceProvider.GetRequiredService<IXsdValidator>();
            var repository = scope.ServiceProvider.GetRequiredService<IOutboxMessageRepository>();

            var recordId = ReadFiscalRecordId(message.Payload);

            // 4. Consultar el registro fiscal utilizando filtros multi-tenant globales.
            var record = await context.FiscalRecords
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == recordId, ct);

            // 5 y 6. Comprobacion de existencia y de aislamiento de tenants.
            if (record is null)
            {
                // Comprobar si el registro pertenece a otro tenant para generar diagnostico explicito.
                var belongsToAnotherTenant = await context.FiscalRecords
                    .IgnoreQueryFilters()
                    .AnyAsync(x => x.Id == recordId && x.TenantId != message.TenantId, ct);

                if (belongsToAnotherTenant)
                {
                    _logger.LogError(
                        "Intento de acceso cruzado entre tenants: el registro {RecordId} pertenece a otro tenant diferente de {TenantId}.",
                        recordId, message.TenantId);

                    await FailScopedAsync(repository, message,
                        $"Violacion de aislamiento multi-tenant: el registro {recordId} pertenece a otro tenant.",
                        permanent: true, ct);
                    return;
                }

                await FailScopedAsync(repository, message,
                    $"El registro fiscal {recordId} no existe.", permanent: true, ct);
                return;
            }

            if (record.TenantId != message.TenantId)
            {
                _logger.LogError(
                    "Discrepancia multi-tenant: FiscalRecord.TenantId ({RecordTenant}) no coincide con OutboxMessage.TenantId ({MessageTenant}).",
                    record.TenantId, message.TenantId);

                await FailScopedAsync(repository, message,
                    "Discrepancia multi-tenant entre registro fiscal y mensaje outbox.",
                    permanent: true, ct);
                return;
            }

            if (string.IsNullOrWhiteSpace(record.XmlPayload))
            {
                await FailScopedAsync(repository, message,
                    "El registro fiscal no tiene XML: la emision no lo ha generado.", permanent: false, ct);
                return;
            }

            var validation = validator.Validate(record.XmlPayload);

            if (!validation.IsValid)
            {
                _logger.LogError(
                    "XML invalido para el registro {RecordId}: {Errors}",
                    recordId, validation.Summary);

                await FailScopedAsync(repository, message, validation.Summary, permanent: true, ct);
                return;
            }

            // 7. Completar dentro del mismo scope y comprobando el ClaimToken.
            await CompleteScopedAsync(repository, message, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Fallo al procesar el mensaje {MessageId}", message.Id);
            await FailDirectAsync(message, ex.Message, permanent: false, ct);
        }
    }

    private async Task CompleteScopedAsync(
        IOutboxMessageRepository repository,
        OutboxMessage message,
        CancellationToken ct)
    {
        var completed = await repository.MarkProcessedAsync(
            message.Id, message.ClaimToken, DateTimeOffset.UtcNow, ct);

        if (!completed)
        {
            _logger.LogWarning(
                "No se pudo completar el mensaje {MessageId}: reclamacion expirada o reasignada a otro worker.",
                message.Id);
        }
    }

    private async Task FailScopedAsync(
        IOutboxMessageRepository repository,
        OutboxMessage message,
        string error,
        bool permanent,
        CancellationToken ct)
    {
        var maxAttempts = permanent ? 1 : _options.MaxAttempts;
        var failed = await repository.MarkFailedAsync(
            message.Id, message.ClaimToken, DateTimeOffset.UtcNow, error, maxAttempts, _options.BaseDelay, ct);

        if (!failed)
        {
            _logger.LogWarning(
                "No se pudo registrar fallo en el mensaje {MessageId}: reclamacion expirada o reasignada a otro worker.",
                message.Id);
        }
    }

    private async Task FailDirectAsync(
        OutboxMessage message,
        string error,
        bool permanent,
        CancellationToken ct)
    {
        try
        {
            await using var scope = _scopes.CreateAsyncScope();
            if (message.TenantId != Guid.Empty)
            {
                SetScopeTenant(scope.ServiceProvider, message.TenantId);
            }

            var repository = scope.ServiceProvider.GetRequiredService<IOutboxMessageRepository>();
            var maxAttempts = permanent ? 1 : _options.MaxAttempts;
            await repository.MarkFailedAsync(
                message.Id, message.ClaimToken, DateTimeOffset.UtcNow, error, maxAttempts, _options.BaseDelay, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Fallo al registrar estado fallido para el mensaje {MessageId}", message.Id);
        }
    }

    private static void SetScopeTenant(IServiceProvider sp, Guid tenantId)
    {
        var tc = sp.GetService<TenantContext>();
        if (tc is not null)
        {
            tc.SetTenant(tenantId);
            return;
        }

        var itc = sp.GetRequiredService<ITenantContext>();
        if (itc is TenantContext concrete)
        {
            concrete.SetTenant(tenantId);
            return;
        }

        var prop = itc.GetType().GetProperty(nameof(ITenantContext.TenantId));
        if (prop is not null && prop.CanWrite)
        {
            prop.SetValue(itc, tenantId);
            return;
        }

        var method = itc.GetType().GetMethod("SetTenant", [typeof(Guid)]);
        if (method is not null)
        {
            method.Invoke(itc, [tenantId]);
            return;
        }

        throw new InvalidOperationException($"No se pudo fijar el tenant en {itc.GetType().Name}.");
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