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

    [Fact]
    public async Task El_primer_worker_con_lease_caducado_intenta_completarlo_y_es_rechazado()
    {
        var message = await SeedAsync();

        // Worker 1 reclama el mensaje con lease ya expirado
        string worker1Token;
        using (var scope1 = _fixture.CreateScope(_tenantId))
        {
            var claimed1 = await scope1.ServiceProvider.GetRequiredService<IOutboxMessageRepository>()
                .ClaimPendingAsync(10, TimeSpan.FromMilliseconds(-1), CancellationToken.None);
            Assert.Single(claimed1);
            worker1Token = claimed1[0].ClaimToken!;
            Assert.NotNull(worker1Token);
        }

        // Worker 2 recupera el mensaje expirado y obtiene un token nuevo
        string worker2Token;
        using (var scope2 = _fixture.CreateScope(_tenantId))
        {
            var claimed2 = await scope2.ServiceProvider.GetRequiredService<IOutboxMessageRepository>()
                .ClaimPendingAsync(10, TimeSpan.FromMinutes(2), CancellationToken.None);
            Assert.Single(claimed2);
            worker2Token = claimed2[0].ClaimToken!;
            Assert.NotEqual(worker1Token, worker2Token);
        }

        // Worker 1 termina tarde e intenta marcar como completado: debe ser RECHAZADO
        using (var scope1 = _fixture.CreateScope(_tenantId))
        {
            var success1 = await scope1.ServiceProvider.GetRequiredService<IOutboxMessageRepository>()
                .MarkProcessedAsync(message.Id, worker1Token, DateTimeOffset.UtcNow, CancellationToken.None);
            Assert.False(success1, "Worker 1 con lease caducado y reasignado no debe poder completar el mensaje.");
        }

        // Worker 2 completa el mensaje exitosamente
        using (var scope2 = _fixture.CreateScope(_tenantId))
        {
            var success2 = await scope2.ServiceProvider.GetRequiredService<IOutboxMessageRepository>()
                .MarkProcessedAsync(message.Id, worker2Token, DateTimeOffset.UtcNow, CancellationToken.None);
            Assert.True(success2, "Worker 2 propietario del lease vigente debe poder completar el mensaje.");
        }

        var stored = await ReadAsync(message.Id);
        Assert.Equal(OutboxStatus.Completed, stored!.Status);
    }

    [Fact]
    public async Task El_primer_worker_con_lease_caducado_intenta_marcarlo_fallido_y_es_rechazado()
    {
        var message = await SeedAsync();

        // Worker 1 reclama
        string worker1Token;
        using (var scope1 = _fixture.CreateScope(_tenantId))
        {
            var claimed1 = await scope1.ServiceProvider.GetRequiredService<IOutboxMessageRepository>()
                .ClaimPendingAsync(10, TimeSpan.FromMilliseconds(-1), CancellationToken.None);
            worker1Token = claimed1[0].ClaimToken!;
        }

        // Worker 2 recupera
        using (var scope2 = _fixture.CreateScope(_tenantId))
        {
            await scope2.ServiceProvider.GetRequiredService<IOutboxMessageRepository>()
                .ClaimPendingAsync(10, TimeSpan.FromMinutes(2), CancellationToken.None);
        }

        // Worker 1 intenta marcarlo como fallido con su token antiguo: debe ser rechazado
        using (var scope1 = _fixture.CreateScope(_tenantId))
        {
            var rejected = await scope1.ServiceProvider.GetRequiredService<IOutboxMessageRepository>()
                .MarkFailedAsync(message.Id, worker1Token, DateTimeOffset.UtcNow, "error tardio", 5, TimeSpan.FromSeconds(10), CancellationToken.None);
            Assert.False(rejected, "Worker 1 no debe poder modificar un mensaje que ya no posee.");
        }
    }

    [Fact]
    public async Task Un_reinicio_no_pierde_mensajes_pendientes()
    {
        var msg1 = await SeedAsync();
        var msg2 = await SeedAsync();

        // msg1 es reclamado y completado
        using (var scope = _fixture.CreateScope(_tenantId))
        {
            var repo = scope.ServiceProvider.GetRequiredService<IOutboxMessageRepository>();
            var claimed = await repo.ClaimPendingAsync(1, TimeSpan.FromMinutes(2), CancellationToken.None);
            await repo.MarkProcessedAsync(claimed[0].Id, claimed[0].ClaimToken, DateTimeOffset.UtcNow, CancellationToken.None);
        }

        // Simula reinicio abriendo un nuevo scope desde cero
        using (var freshScope = _fixture.CreateScope(_tenantId))
        {
            var repo = freshScope.ServiceProvider.GetRequiredService<IOutboxMessageRepository>();
            var pending = await repo.ClaimPendingAsync(10, TimeSpan.FromMinutes(2), CancellationToken.None);

            // msg2 permanece pendiente y es recuperado intacto
            Assert.Single(pending);
            Assert.Equal(msg2.Id, pending[0].Id);
        }
    }

    // ---------- Estados terminales y propiedad de ClaimToken ----------

    [Fact]
    public async Task Marcar_procesado_con_claim_token_valido_deja_el_mensaje_completado()
    {
        var message = await SeedAsync();

        using var scope = _fixture.CreateScope(_tenantId);
        var repo = scope.ServiceProvider.GetRequiredService<IOutboxMessageRepository>();
        var claimed = await repo.ClaimPendingAsync(1, TimeSpan.FromMinutes(2), CancellationToken.None);
        Assert.Single(claimed);

        var success = await repo.MarkProcessedAsync(message.Id, claimed[0].ClaimToken, DateTimeOffset.UtcNow, CancellationToken.None);
        Assert.True(success);

        var stored = await ReadAsync(message.Id);
        Assert.Equal(OutboxStatus.Completed, stored!.Status);
        Assert.NotNull(stored.ProcessedAt);
        Assert.Null(stored.NextAttemptAt);
        Assert.Null(stored.ClaimToken);
    }

    [Fact]
    public async Task Modificar_mensaje_sin_claim_token_valido_es_rechazado()
    {
        var message = await SeedAsync();

        using var scope = _fixture.CreateScope(_tenantId);
        var repo = scope.ServiceProvider.GetRequiredService<IOutboxMessageRepository>();

        // Token nulo: rechazado
        var nullProcessed = await repo.MarkProcessedAsync(message.Id, null, DateTimeOffset.UtcNow, CancellationToken.None);
        Assert.False(nullProcessed);

        // Token vacio: rechazado
        var emptyProcessed = await repo.MarkProcessedAsync(message.Id, "   ", DateTimeOffset.UtcNow, CancellationToken.None);
        Assert.False(emptyProcessed);

        // Token invalido: rechazado
        var wrongProcessed = await repo.MarkProcessedAsync(message.Id, "token-invalido", DateTimeOffset.UtcNow, CancellationToken.None);
        Assert.False(wrongProcessed);

        // Fallo con token nulo: rechazado
        var nullFailed = await repo.MarkFailedAsync(message.Id, null, DateTimeOffset.UtcNow, "error", 5, TimeSpan.FromSeconds(10), CancellationToken.None);
        Assert.False(nullFailed);

        var stored = await ReadAsync(message.Id);
        Assert.Equal(OutboxStatus.Pending, stored!.Status);
    }

    [Fact]
    public async Task Un_mensaje_completado_no_se_reclama_nunca_mas()
    {
        var message = await SeedAsync();
        using (var scope = _fixture.CreateScope(_tenantId))
        {
            var repo = scope.ServiceProvider.GetRequiredService<IOutboxMessageRepository>();
            var claimed = await repo.ClaimPendingAsync(1, TimeSpan.FromMinutes(2), CancellationToken.None);
            await repo.MarkProcessedAsync(message.Id, claimed[0].ClaimToken, DateTimeOffset.UtcNow, CancellationToken.None);
        }

        using var again = _fixture.CreateScope(_tenantId);
        var againClaimed = await again.ServiceProvider.GetRequiredService<IOutboxMessageRepository>()
            .ClaimPendingAsync(10, TimeSpan.FromMinutes(2), CancellationToken.None);

        Assert.Empty(againClaimed);
    }

    // ---------- Reintentos ----------

    [Fact]
    public async Task Un_fallo_reprograma_el_reintento_y_guarda_el_error()
    {
        var message = await SeedAsync();

        using var scope = _fixture.CreateScope(_tenantId);
        var repo = scope.ServiceProvider.GetRequiredService<IOutboxMessageRepository>();
        var claimed = await repo.ClaimPendingAsync(1, TimeSpan.FromMinutes(2), CancellationToken.None);
        Assert.Single(claimed);

        var success = await repo.MarkFailedAsync(message.Id, claimed[0].ClaimToken, DateTimeOffset.UtcNow, "fallo transitorio",
            maxAttempts: 5, baseDelay: TimeSpan.FromSeconds(30), CancellationToken.None);
        Assert.True(success);

        var stored = await ReadAsync(message.Id);
        Assert.Equal(OutboxStatus.Pending, stored!.Status);
        Assert.Equal(1, stored.Attempts);
        Assert.Equal("fallo transitorio", stored.LastError);
        Assert.Null(stored.ClaimToken);
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
            var repo = scope.ServiceProvider.GetRequiredService<IOutboxMessageRepository>();
            var claimed = await repo.ClaimPendingAsync(1, TimeSpan.FromMinutes(2), CancellationToken.None);
            Assert.Single(claimed);

            await repo.MarkFailedAsync(message.Id, claimed[0].ClaimToken, DateTimeOffset.UtcNow, "fallo",
                maxAttempts: 5, baseDelay: TimeSpan.FromSeconds(30), CancellationToken.None);

            var stored = await ReadAsync(message.Id);
            delays.Add(stored!.NextAttemptAt!.Value - DateTimeOffset.UtcNow);

            // Reestablecer NextAttemptAt a null para permitir la siguiente reclamacion
            using var resetScope = _fixture.CreateScope(_tenantId);
            var db = resetScope.ServiceProvider.GetRequiredService<BillingDbContext>();
            await db.OutboxMessages.Where(x => x.Id == message.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.NextAttemptAt, (DateTimeOffset?)null));
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
            var repo = scope.ServiceProvider.GetRequiredService<IOutboxMessageRepository>();
            var claimed = await repo.ClaimPendingAsync(1, TimeSpan.FromMinutes(2), CancellationToken.None);
            Assert.Single(claimed);

            await repo.MarkFailedAsync(message.Id, claimed[0].ClaimToken, DateTimeOffset.UtcNow, "invalido",
                maxAttempts: 1, baseDelay: TimeSpan.FromSeconds(30), CancellationToken.None);
        }

        var stored = await ReadAsync(message.Id);
        Assert.Equal(OutboxStatus.Failed, stored!.Status);
        Assert.Null(stored.NextAttemptAt);

        // Agotado, no vuelve a la cola.
        using var again = _fixture.CreateScope(_tenantId);
        var againClaimed = await again.ServiceProvider.GetRequiredService<IOutboxMessageRepository>()
            .ClaimPendingAsync(10, TimeSpan.FromMinutes(2), CancellationToken.None);

        Assert.Empty(againClaimed);
    }

    [Fact]
    public async Task Leases_vigentes_no_bloquean_la_seleccion_de_mensajes_pendientes()
    {
        // 1. Mensaje en Processing con lease activo en el futuro (1 hora)
        var activeProcessing = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Type = FiscalOutboxType,
            AggregateId = Guid.NewGuid().ToString(),
            Payload = "{}",
            Status = OutboxStatus.Processing,
            ClaimToken = "active-token",
            CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-10),
            NextAttemptAt = DateTimeOffset.UtcNow.AddHours(1)
        };

        // 2. Mensaje en Pending listo para ser procesado
        var eligiblePending = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Type = FiscalOutboxType,
            AggregateId = Guid.NewGuid().ToString(),
            Payload = "{}",
            Status = OutboxStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-5),
            NextAttemptAt = null
        };

        using (var scope = _fixture.CreateScope(_tenantId))
        {
            var db = scope.ServiceProvider.GetRequiredService<BillingDbContext>();
            await db.OutboxMessages.AddRangeAsync(activeProcessing, eligiblePending);
            await db.SaveChangesAsync();
        }

        using (var claimScope = _fixture.CreateScope(_tenantId))
        {
            var repo = claimScope.ServiceProvider.GetRequiredService<IOutboxMessageRepository>();
            var claimed = await repo.ClaimPendingAsync(1, TimeSpan.FromMinutes(2), CancellationToken.None);

            Assert.Single(claimed);
            Assert.Equal(eligiblePending.Id, claimed[0].Id);
        }
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