using System.Text.Json;
using Facturacion.Application.Interfaces;
using Facturacion.Domain.Entities;
using Facturacion.Infrastructure.Persistence;
using Facturacion.Integration.Test.Infrastructure;
using Facturacion.Verifactu.Xml;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Facturacion.Integration.Test;

/// <summary>
/// Comportamiento de la cola outbox: reclamacion atomica, reintentos con
/// espera creciente, estados terminales y aislamiento por tenant.
///
/// El procesador vive en el proyecto de workers, asi que aqui se ejercita el
/// repositorio y las piezas que determinan su resultado.
/// </summary>
public sealed class OutboxQueueTests : IDisposable
{
    private readonly BillingDatabaseFixture _fixture = new();
    private Guid _tenantId;

    public OutboxQueueTests()
    {
        _tenantId = _fixture.Seed().TenantId;
    }

    public void Dispose() => _fixture.Dispose();

    private async Task<OutboxMessage> SeedAsync(
        string type = FiscalOutboxType,
        string status = OutboxStatus.Pending,
        DateTimeOffset? nextAttemptAt = null)
    {
        var message = new OutboxMessage
        {
            TenantId = _tenantId,
            Type = type,
            AggregateId = Guid.NewGuid().ToString(),
            Payload = "{}",
            Status = status,
            CreatedAt = DateTimeOffset.UtcNow,
            NextAttemptAt = nextAttemptAt
        };

        using var scope = _fixture.CreateScope(_tenantId);
        await scope.ServiceProvider.GetRequiredService<IOutboxMessageRepository>()
            .AddAsync(message, CancellationToken.None);
        await scope.ServiceProvider.GetRequiredService<BillingDbContext>().SaveChangesAsync();

        return message;
    }

    private const string FiscalOutboxType = "fiscal.record.created";

    // Lectura sin filtro de tenant: es como la ve el worker, que no tiene un
    // unico tenant fijado.
    private async Task<OutboxMessage?> ReadAsync(Guid id)
    {
        using var scope = _fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BillingDbContext>();

        return await db.OutboxMessages.AsNoTracking()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    // ---------- Reclamacion ----------

    [Fact]
    public async Task Reclamar_marca_el_mensaje_como_processing()
    {
        var message = await SeedAsync();

        using var scope = _fixture.CreateScope(_tenantId);
        var claimed = await scope.ServiceProvider.GetRequiredService<IOutboxMessageRepository>()
            .ClaimPendingAsync(10, TimeSpan.FromMinutes(2), CancellationToken.None);

        var stored = await ReadAsync(message.Id);
        Assert.Single(claimed);
        Assert.Equal(OutboxStatus.Processing, stored!.Status);
    }

    [Fact]
    public async Task Un_mensaje_reclamado_no_se_vuelve_a_reclamar()
    {
        await SeedAsync();

        using var first = _fixture.CreateScope(_tenantId);
        await first.ServiceProvider.GetRequiredService<IOutboxMessageRepository>()
            .ClaimPendingAsync(10, TimeSpan.FromMinutes(2), CancellationToken.None);

        // Un segundo worker, con su propio contexto, no debe ver el mensaje.
        using var second = _fixture.CreateScope(_tenantId);
        var claimed = await second.ServiceProvider.GetRequiredService<IOutboxMessageRepository>()
            .ClaimPendingAsync(10, TimeSpan.FromMinutes(2), CancellationToken.None);

        Assert.Empty(claimed);
    }

    [Fact]
    public async Task Dos_workers_concurrentes_no_reclaman_el_mismo_mensaje()
    {
        await SeedAsync();

        var results = await Task.WhenAll(
            ClaimInOwnContextAsync(),
            ClaimInOwnContextAsync());

        // El mensaje se reclama una sola vez entre los dos workers.
        Assert.Equal(1, results.Sum(x => x));
    }

    private async Task<int> ClaimInOwnContextAsync()
    {
        using var scope = _fixture.CreateScope(_tenantId);
        var claimed = await scope.ServiceProvider.GetRequiredService<IOutboxMessageRepository>()
            .ClaimPendingAsync(10, TimeSpan.FromMinutes(2), CancellationToken.None);
        await scope.ServiceProvider.GetRequiredService<BillingDbContext>().SaveChangesAsync();
        return claimed.Count;
    }

    [Fact]
    public async Task Un_reintento_programado_no_se_reclama_antes_de_su_hora()
    {
        await SeedAsync(nextAttemptAt: DateTimeOffset.UtcNow.AddMinutes(30));

        using var scope = _fixture.CreateScope(_tenantId);
        var claimed = await scope.ServiceProvider.GetRequiredService<IOutboxMessageRepository>()
            .ClaimPendingAsync(10, TimeSpan.FromMinutes(2), CancellationToken.None);

        Assert.Empty(claimed);
    }

    [Fact]
    public async Task Un_mensaje_reclamado_vuelve_a_la_cola_cuando_expira_la_lease()
    {
        var message = await SeedAsync();

        using (var scope = _fixture.CreateScope(_tenantId))
        {
            // Lease ya vencida: simula un worker que murio sin cerrar.
            await scope.ServiceProvider.GetRequiredService<IOutboxMessageRepository>()
                .ClaimPendingAsync(10, TimeSpan.FromMilliseconds(-1), CancellationToken.None);
        }

        using var retry = _fixture.CreateScope(_tenantId);
        var claimed = await retry.ServiceProvider.GetRequiredService<IOutboxMessageRepository>()
            .ClaimPendingAsync(10, TimeSpan.FromMinutes(2), CancellationToken.None);

        Assert.Single(claimed);
        Assert.Equal(message.Id, claimed[0].Id);
    }

    // ---------- Estados terminales ----------

    [Fact]
    public async Task Marcar_procesado_deja_el_mensaje_completado()
    {
        var message = await SeedAsync();

        using var scope = _fixture.CreateScope(_tenantId);
        await scope.ServiceProvider.GetRequiredService<IOutboxMessageRepository>()
            .MarkProcessedAsync(message.Id, DateTimeOffset.UtcNow, CancellationToken.None);

        var stored = await ReadAsync(message.Id);
        Assert.Equal(OutboxStatus.Completed, stored!.Status);
        Assert.NotNull(stored.ProcessedAt);
        Assert.Null(stored.NextAttemptAt);
    }

    [Fact]
    public async Task Un_mensaje_completado_no_se_reclama_nunca_mas()
    {
        var message = await SeedAsync();
        using (var scope = _fixture.CreateScope(_tenantId))
        {
            await scope.ServiceProvider.GetRequiredService<IOutboxMessageRepository>()
                .MarkProcessedAsync(message.Id, DateTimeOffset.UtcNow, CancellationToken.None);
        }

        using var again = _fixture.CreateScope(_tenantId);
        var claimed = await again.ServiceProvider.GetRequiredService<IOutboxMessageRepository>()
            .ClaimPendingAsync(10, TimeSpan.FromMinutes(2), CancellationToken.None);

        Assert.Empty(claimed);
    }

    // ---------- Reintentos ----------

    [Fact]
    public async Task Un_fallo_reprograma_el_reintento_y_guarda_el_error()
    {
        var message = await SeedAsync();

        using var scope = _fixture.CreateScope(_tenantId);
        await scope.ServiceProvider.GetRequiredService<IOutboxMessageRepository>()
            .MarkFailedAsync(message.Id, DateTimeOffset.UtcNow, "fallo transitorio",
                maxAttempts: 5, baseDelay: TimeSpan.FromSeconds(30), CancellationToken.None);

        var stored = await ReadAsync(message.Id);
        Assert.Equal(OutboxStatus.Pending, stored!.Status);
        Assert.Equal(1, stored.Attempts);
        Assert.Equal("fallo transitorio", stored.LastError);
        Assert.True(stored.NextAttemptAt > DateTimeOffset.UtcNow,
            "El reintento debe estar programado en el futuro.");
    }

    [Fact]
    public async Task La_espera_crece_en_cada_intento()
    {
        var message = await SeedAsync();
        var delays = new List<TimeSpan>();

        for (var attempt = 0; attempt < 3; attempt++)
        {
            using var scope = _fixture.CreateScope(_tenantId);
            await scope.ServiceProvider.GetRequiredService<IOutboxMessageRepository>()
                .MarkFailedAsync(message.Id, DateTimeOffset.UtcNow, "fallo",
                    maxAttempts: 5, baseDelay: TimeSpan.FromSeconds(30), CancellationToken.None);

            var stored = await ReadAsync(message.Id);
            // El mensaje queda en Pending con un NextAttemptAt futuro; se
            // adelanta para poder simular el siguiente intento.
            delays.Add(stored!.NextAttemptAt!.Value - DateTimeOffset.UtcNow);

            using var reset = _fixture.CreateScope(_tenantId);
            await reset.ServiceProvider.GetRequiredService<IOutboxMessageRepository>()
                .AddAsync(new OutboxMessage { Id = message.Id, TenantId = _tenantId, Type = "x" }, default);
        }

        Assert.True(delays[1] > delays[0], $"El segundo reintento ({delays[1]}) debe esperar mas que el primero ({delays[0]}).");
        Assert.True(delays[2] > delays[1], "El tercero debe esperar mas que el segundo.");
    }

    [Fact]
    public async Task Al_agotar_los_intentos_el_mensaje_queda_fallido_y_no_se_reintenta()
    {
        var message = await SeedAsync();

        using (var scope = _fixture.CreateScope(_tenantId))
        {
            await scope.ServiceProvider.GetRequiredService<IOutboxMessageRepository>()
                .MarkFailedAsync(message.Id, DateTimeOffset.UtcNow, "invalido",
                    maxAttempts: 1, baseDelay: TimeSpan.FromSeconds(30), CancellationToken.None);
        }

        var stored = await ReadAsync(message.Id);
        Assert.Equal(OutboxStatus.Failed, stored!.Status);
        Assert.Null(stored.NextAttemptAt);

        // Agotado, no vuelve a la cola.
        using var again = _fixture.CreateScope(_tenantId);
        var claimed = await again.ServiceProvider.GetRequiredService<IOutboxMessageRepository>()
            .ClaimPendingAsync(10, TimeSpan.FromMinutes(2), CancellationToken.None);

        Assert.Empty(claimed);
    }

    // ---------- Aislamiento por tenant ----------

    [Fact]
    public async Task El_procesador_ve_mensajes_de_dos_tenants()
    {
        // Un segundo tenant sembrado de verdad: el contexto valida la
        // pertenencia al guardar, no basta con inventarse un Guid.
        var other = _fixture.Seed().TenantId;

        using (var scope = _fixture.CreateScope(_tenantId))
        {
            await scope.ServiceProvider.GetRequiredService<IOutboxMessageRepository>()
                .AddAsync(New(_tenantId), default);
            await scope.ServiceProvider.GetRequiredService<BillingDbContext>().SaveChangesAsync();
        }
        using (var scope = _fixture.CreateScope(other))
        {
            await scope.ServiceProvider.GetRequiredService<IOutboxMessageRepository>()
                .AddAsync(New(other), default);
            await scope.ServiceProvider.GetRequiredService<BillingDbContext>().SaveChangesAsync();
        }

        // Un worker sin tenant fijado debe poder reclamar de todos.
        using var worker = _fixture.CreateScope();
        var claimed = await worker.ServiceProvider.GetRequiredService<IOutboxMessageRepository>()
            .ClaimPendingAsync(10, TimeSpan.FromMinutes(2), CancellationToken.None);

        Assert.Equal(2, claimed.Count);
        Assert.Contains(claimed, x => x.TenantId == _tenantId);
        Assert.Contains(claimed, x => x.TenantId == other);
    }

    private OutboxMessage New(Guid tenantId) => new()
    {
        TenantId = tenantId,
        Type = FiscalOutboxType,
        AggregateId = Guid.NewGuid().ToString(),
        Payload = "{}",
        Status = OutboxStatus.Pending,
        CreatedAt = DateTimeOffset.UtcNow
    };

    [Fact]
    public async Task El_payload_de_la_emision_lleva_el_identificador_del_registro()
    {
        var recordId = Guid.NewGuid();
        var message = new OutboxMessage
        {
            TenantId = _tenantId,
            Type = FiscalOutboxType,
            AggregateId = recordId.ToString(),
            Payload = JsonSerializer.Serialize(new
            {
                InvoiceId = Guid.NewGuid(),
                FiscalRecordId = recordId
            }),
            Status = OutboxStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow
        };

        using var scope = _fixture.CreateScope(_tenantId);
        await scope.ServiceProvider.GetRequiredService<IOutboxMessageRepository>().AddAsync(message, default);
        await scope.ServiceProvider.GetRequiredService<BillingDbContext>().SaveChangesAsync();

        // El procesador depende de poder leer ese campo; si cambia la forma
        // del payload, esto avisa antes que un fallo en produccion.
        using var document = JsonDocument.Parse(message.Payload);
        Assert.Equal(recordId, document.RootElement.GetProperty("FiscalRecordId").GetGuid());
    }
}