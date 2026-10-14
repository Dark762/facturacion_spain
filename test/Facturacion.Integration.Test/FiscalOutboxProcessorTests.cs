using System.Text.Json;
using Facturacion.Application.DTOs.Invoices;
using Facturacion.Application.Interfaces;
using Facturacion.Configuration.Options;
using Facturacion.Domain.Entities;
using Facturacion.Infrastructure.Persistence;
using Facturacion.Integration.Test.Infrastructure;
using Facturacion.Verifactu.Xml;
using Facturacion.Workers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Facturacion.Integration.Test;

/// <summary>
/// Pruebas obligatorias de la Fase 1 (aislamiento multi-tenant)
/// y Fase 2 (prevencion de perdida de mensajes en Outbox).
/// </summary>
public sealed class FiscalOutboxProcessorTests : IDisposable
{
    private readonly BillingDatabaseFixture _fixture = new();
    private readonly BillingDatabaseFixture.SeededCompany _companyA;
    private readonly BillingDatabaseFixture.SeededCompany _companyB;

    public FiscalOutboxProcessorTests()
    {
        _companyA = _fixture.Seed();
        _companyB = _fixture.Seed();
    }

    public void Dispose() => _fixture.Dispose();

    private FiscalOutboxProcessor CreateProcessor(OutboxOptions? options = null)
    {
        var scope = _fixture.CreateScope();
        var scopeFactory = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();
        return new FiscalOutboxProcessor(
            scopeFactory,
            NullLogger<FiscalOutboxProcessor>.Instance,
            options ?? new OutboxOptions { BatchSize = 50, Lease = TimeSpan.FromMinutes(2), BaseDelay = TimeSpan.FromSeconds(1) });
    }

    private async Task<Guid> CreateDraftInvoiceAsync(BillingDatabaseFixture.SeededCompany company)
    {
        using var scope = _fixture.CreateScope(company.TenantId);
        var invoices = scope.ServiceProvider.GetRequiredService<IInvoiceApplicationService>();
        var draft = new CreateInvoiceRequest(
            company.CompanyId,
            company.CustomerId,
            new DateOnly(2026, 3, 15),
            "EUR",
            [new CreateInvoiceLineRequest("Servicios", 1m, 100m, 21m)]);
        var created = await invoices.CreateDraftAsync(draft, CancellationToken.None);
        return created.Value.Id;
    }

    private async Task<(FiscalRecord Record, OutboxMessage Message)> SeedFiscalRecordAndMessageAsync(
        BillingDatabaseFixture.SeededCompany company,
        bool withValidXml = true)
    {
        var recordId = Guid.NewGuid();
        var invoiceId = await CreateDraftInvoiceAsync(company);

        var xml = withValidXml ? GenerateValidSubmissionXml() : "<invalido>no-valida</invalido>";

        var fiscalRecord = new FiscalRecord
        {
            Id = recordId,
            TenantId = company.TenantId,
            InvoiceId = invoiceId,
            RecordType = Facturacion.Domain.Enums.FiscalRecordType.Alta,
            SoftwareName = "FacturacionSaaS",
            SoftwareVersion = "1.0",
            InstallationId = "INST-1",
            Hash = "3C464DAF61ACB827C65FDA19F352A4E3BDC2C640E9E9FC4CC058073F38F12F60",
            XmlPayload = xml,
            GeneratedAt = DateTimeOffset.UtcNow
        };

        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            TenantId = company.TenantId,
            Type = FiscalOutboxProcessor.FiscalRecordCreated,
            AggregateId = recordId.ToString(),
            Payload = JsonSerializer.Serialize(new
            {
                InvoiceId = invoiceId,
                FiscalRecordId = recordId
            }),
            Status = OutboxStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow
        };

        using var scope = _fixture.CreateScope(company.TenantId);
        var db = scope.ServiceProvider.GetRequiredService<BillingDbContext>();
        await db.FiscalRecords.AddAsync(fiscalRecord);
        await db.OutboxMessages.AddAsync(message);
        await db.SaveChangesAsync();

        return (fiscalRecord, message);
    }

    private static string GenerateValidSubmissionXml()
    {
        var builder = new AeatRecordXmlBuilder();
        var data = new FiscalRecordXmlData
        {
            IssuerNif = "89890001K",
            IssuerName = "EMPRESA PRUEBA",
            InvoiceNumber = "A/100",
            IssueDate = new DateOnly(2024, 1, 1),
            InvoiceType = "F1",
            Description = "Prestacion servicios",
            IsFirstRecord = true,
            SystemName = "FacturacionSaaS",
            SystemId = "TS",
            SystemVersion = "1.0.0",
            InstallationNumber = "INST-001",
            ProducerName = "PRODUCTORA",
            ProducerNif = "B12345678",
            TaxLines = [new TaxBreakdownLine("01", 100m, 21m, 21m)],
            TaxTotal = 21m,
            Total = 121m,
            Hash = "3C464DAF61ACB827C65FDA19F352A4E3BDC2C640E9E9FC4CC058073F38F12F60",
            GeneratedAt = new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.Zero)
        };

        var altaXml = builder.BuildAltaXml(data);
        return builder.BuildSubmissionXml("EMPRESA PRUEBA", "89890001K", altaXml);
    }

    private async Task<OutboxMessage?> ReadMessageAsync(Guid id)
    {
        using var scope = _fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BillingDbContext>();
        return await db.OutboxMessages.AsNoTracking().IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == id);
    }

    // =========================================================================
    // FASE 1: AISLAMIENTO MULTI-TENANT DEL WORKER
    // =========================================================================

    [Fact]
    public async Task Procesar_mensajes_de_dos_tenants_diferentes_aisla_correctamente_ambos()
    {
        var (_, msgA) = await SeedFiscalRecordAndMessageAsync(_companyA);
        var (_, msgB) = await SeedFiscalRecordAndMessageAsync(_companyB);

        var processor = CreateProcessor();
        var processed = await processor.ProcessBatchAsync(CancellationToken.None);

        Assert.Equal(2, processed);

        var storedA = await ReadMessageAsync(msgA.Id);
        var storedB = await ReadMessageAsync(msgB.Id);

        Assert.Equal(OutboxStatus.Completed, storedA!.Status);
        Assert.Equal(OutboxStatus.Completed, storedB!.Status);
    }

    [Fact]
    public async Task Intentar_acceder_al_registro_fiscal_de_otro_tenant_falla_por_violacion_de_aislamiento()
    {
        var (recB, _) = await SeedFiscalRecordAndMessageAsync(_companyB);

        // Mensaje perteneciente a Tenant A que intenta apuntar al FiscalRecord de Tenant B
        var maliciousMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            TenantId = _companyA.TenantId,
            Type = FiscalOutboxProcessor.FiscalRecordCreated,
            AggregateId = recB.Id.ToString(),
            Payload = JsonSerializer.Serialize(new
            {
                InvoiceId = Guid.NewGuid(),
                FiscalRecordId = recB.Id
            }),
            Status = OutboxStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow
        };

        using (var scope = _fixture.CreateScope(_companyA.TenantId))
        {
            var db = scope.ServiceProvider.GetRequiredService<BillingDbContext>();
            await db.OutboxMessages.AddAsync(maliciousMessage);
            await db.SaveChangesAsync();
        }

        var processor = CreateProcessor();
        await processor.ProcessBatchAsync(CancellationToken.None);

        var stored = await ReadMessageAsync(maliciousMessage.Id);
        Assert.Equal(OutboxStatus.Failed, stored!.Status);
        Assert.Contains("otro tenant", stored.LastError, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Procesar_un_mensaje_con_tenant_inexistente_falla_permanentemente()
    {
        var msgId = Guid.NewGuid();
        var emptyTenant = Guid.Empty.ToString();

        using (var scope = _fixture.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BillingDbContext>();
            // Usamos SQL crudo para insertar TenantId vacio sin que ValidateTenant() de EF lo auto-asigne
            await db.Database.ExecuteSqlRawAsync(
                "INSERT INTO OutboxMessages (Id, TenantId, Type, AggregateId, Payload, Status, Attempts, CreatedAt) " +
                "VALUES ({0}, {1}, {2}, {3}, {4}, {5}, 0, {6})",
                msgId, emptyTenant, FiscalOutboxProcessor.FiscalRecordCreated, Guid.NewGuid().ToString(),
                JsonSerializer.Serialize(new { FiscalRecordId = Guid.NewGuid() }), OutboxStatus.Pending, DateTimeOffset.UtcNow);
        }

        var processor = CreateProcessor();
        await processor.ProcessBatchAsync(CancellationToken.None);

        var stored = await ReadMessageAsync(msgId);
        Assert.Equal(OutboxStatus.Failed, stored!.Status);
        Assert.Contains("TenantId valido", stored.LastError);
    }

    [Fact]
    public async Task Verificar_que_un_error_en_un_tenant_no_afecta_a_los_demas()
    {
        // Tenant A tiene un mensaje cuyo registro no existe (fallara)
        var badMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            TenantId = _companyA.TenantId,
            Type = FiscalOutboxProcessor.FiscalRecordCreated,
            AggregateId = Guid.NewGuid().ToString(),
            Payload = JsonSerializer.Serialize(new { FiscalRecordId = Guid.NewGuid() }),
            Status = OutboxStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow
        };

        using (var scope = _fixture.CreateScope(_companyA.TenantId))
        {
            var db = scope.ServiceProvider.GetRequiredService<BillingDbContext>();
            await db.OutboxMessages.AddAsync(badMessage);
            await db.SaveChangesAsync();
        }

        // Tenant B tiene un registro fiscal valido
        var (_, goodMessage) = await SeedFiscalRecordAndMessageAsync(_companyB);

        var processor = CreateProcessor();
        var count = await processor.ProcessBatchAsync(CancellationToken.None);

        Assert.Equal(2, count);

        var storedBad = await ReadMessageAsync(badMessage.Id);
        var storedGood = await ReadMessageAsync(goodMessage.Id);

        Assert.Equal(OutboxStatus.Failed, storedBad!.Status);
        Assert.Equal(OutboxStatus.Completed, storedGood!.Status);
    }

    // =========================================================================
    // FASE 2: IMPEDIR PERDIDA DE EVENTOS OUTBOX NO FISCALES
    // =========================================================================

    [Fact]
    public async Task Un_evento_B2B_permanece_pendiente_cuando_se_ejecuta_el_procesador_fiscal()
    {
        var b2bMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            TenantId = _companyA.TenantId,
            Type = "b2b.transmission.created",
            AggregateId = Guid.NewGuid().ToString(),
            Payload = "{\"TransmissionId\":\"123\"}",
            Status = OutboxStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow
        };

        using (var scope = _fixture.CreateScope(_companyA.TenantId))
        {
            var db = scope.ServiceProvider.GetRequiredService<BillingDbContext>();
            await db.OutboxMessages.AddAsync(b2bMessage);
            await db.SaveChangesAsync();
        }

        var processor = CreateProcessor();
        await processor.ProcessBatchAsync(CancellationToken.None);

        var stored = await ReadMessageAsync(b2bMessage.Id);
        Assert.Equal(OutboxStatus.Pending, stored!.Status);
        Assert.Null(stored.ProcessedAt);
    }

    [Fact]
    public async Task Un_evento_de_invitacion_no_es_consumido_por_el_procesador_fiscal()
    {
        var invitationMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            TenantId = _companyA.TenantId,
            Type = "tenant.invitation.sent",
            AggregateId = Guid.NewGuid().ToString(),
            Payload = "{\"Email\":\"user@test.org\"}",
            Status = OutboxStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow
        };

        using (var scope = _fixture.CreateScope(_companyA.TenantId))
        {
            var db = scope.ServiceProvider.GetRequiredService<BillingDbContext>();
            await db.OutboxMessages.AddAsync(invitationMessage);
            await db.SaveChangesAsync();
        }

        var processor = CreateProcessor();
        await processor.ProcessBatchAsync(CancellationToken.None);

        var stored = await ReadMessageAsync(invitationMessage.Id);
        Assert.Equal(OutboxStatus.Pending, stored!.Status);
    }

    [Fact]
    public async Task Un_evento_desconocido_no_se_pierde_ni_se_marca_como_completado()
    {
        var unknownMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            TenantId = _companyA.TenantId,
            Type = "custom.future.event",
            AggregateId = Guid.NewGuid().ToString(),
            Payload = "{\"key\":\"val\"}",
            Status = OutboxStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow
        };

        using (var scope = _fixture.CreateScope(_companyA.TenantId))
        {
            var db = scope.ServiceProvider.GetRequiredService<BillingDbContext>();
            await db.OutboxMessages.AddAsync(unknownMessage);
            await db.SaveChangesAsync();
        }

        var processor = CreateProcessor();
        await processor.ProcessBatchAsync(CancellationToken.None);

        var stored = await ReadMessageAsync(unknownMessage.Id);
        Assert.Equal(OutboxStatus.Pending, stored!.Status);
    }

    [Fact]
    public async Task Dos_procesadores_con_distinto_tipo_reclaman_solamente_sus_eventos()
    {
        var (_, fiscalMsg) = await SeedFiscalRecordAndMessageAsync(_companyA);

        var b2bMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            TenantId = _companyA.TenantId,
            Type = "b2b.transmission.created",
            AggregateId = Guid.NewGuid().ToString(),
            Payload = "{}",
            Status = OutboxStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow
        };

        using (var scope = _fixture.CreateScope(_companyA.TenantId))
        {
            var db = scope.ServiceProvider.GetRequiredService<BillingDbContext>();
            await db.OutboxMessages.AddAsync(b2bMessage);
            await db.SaveChangesAsync();
        }

        using var scopeWorker = _fixture.CreateScope();
        var repo = scopeWorker.ServiceProvider.GetRequiredService<IOutboxMessageRepository>();

        // Reclamacion fiscal
        var fiscalClaimed = await repo.ClaimPendingAsync(
            10, TimeSpan.FromMinutes(2), FiscalOutboxProcessor.FiscalRecordCreated, CancellationToken.None);

        // Reclamacion B2B
        var b2bClaimed = await repo.ClaimPendingAsync(
            10, TimeSpan.FromMinutes(2), "b2b.transmission.created", CancellationToken.None);

        Assert.Single(fiscalClaimed);
        Assert.Equal(fiscalMsg.Id, fiscalClaimed[0].Id);

        Assert.Single(b2bClaimed);
        Assert.Equal(b2bMessage.Id, b2bClaimed[0].Id);
    }
}
