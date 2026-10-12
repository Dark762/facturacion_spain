using Facturacion.Application.DTOs.Invoices;
using Facturacion.Application.Interfaces;
using Facturacion.Application.Services;
using Facturacion.Domain.Enums;
using Facturacion.Infrastructure.Persistence;
using Facturacion.Integration.Test.Infrastructure;
using Facturacion.Verifactu;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Facturacion.Integration.Test;

/// <summary>
/// La emision debe seguir BLOQUEADA mientras el motor fiscal no este completo,
/// y debe hacerlo SIN consumir numeracion ni dejar residuos.
/// </summary>
public sealed class FiscalEngineFailClosedTests : IClassFixture<BillingDatabaseFixture>
{
    private readonly BillingDatabaseFixture _fixture;

    public FiscalEngineFailClosedTests(BillingDatabaseFixture fixture) => _fixture = fixture;

    private static CreateInvoiceRequest Draft(BillingDatabaseFixture.SeededCompany s) => new(
        s.CompanyId, s.CustomerId, new DateOnly(2026, 3, 15), "EUR",
        new[] { new CreateInvoiceLineRequest("Consultoria", 2m, 50m, 21m) });

    /// <summary>Construye el servicio con el motor REAL, que esta bloqueado.</summary>
    private InvoiceIssuanceService BuildBlockedService(IServiceScope scope, Guid tenantId)
    {
        var sp = scope.ServiceProvider;
        return new InvoiceIssuanceService(
            sp.GetRequiredService<IInvoiceRepository>(),
            sp.GetRequiredService<IInvoiceSequenceRepository>(),
            sp.GetRequiredService<IUnitOfWork>(),
            sp.GetRequiredService<ICompanyAuthorizationService>(),
            sp.GetRequiredService<IOutboxMessageRepository>(),
            sp.GetRequiredService<IFiscalRecordRepository>(),
            sp.GetRequiredService<IFiscalChainRepository>(),
            new BlockedFiscalEngine(),
            sp.GetRequiredService<ITenantContext>());
    }

    [Fact]
    public void El_motor_real_bloquea_la_emision()
    {
        Assert.False(new BlockedFiscalEngine().IsReadyForIssuance);
    }

    [Fact]
    public async Task Con_el_motor_bloqueado_la_emision_se_rechaza_por_codigo_conocido()
    {
        var seeded = _fixture.Seed();
        using var scope = _fixture.CreateScope(seeded.TenantId);

        var invoices = scope.ServiceProvider.GetRequiredService<IInvoiceApplicationService>();
        var created = await invoices.CreateDraftAsync(Draft(seeded), CancellationToken.None);
        Assert.True(created.IsSuccess);

        var service = BuildBlockedService(scope, seeded.TenantId);
        var result = await service.IssueAsync(created.Value.Id, "BLOQ", CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("FISCAL_ENGINE_NOT_CONFIGURED", result.Errors[0].Code);
    }

    [Fact]
    public async Task El_rechazo_no_consume_numeracion()
    {
        var seeded = _fixture.Seed();
        using var scope = _fixture.CreateScope(seeded.TenantId);
        var db = _fixture.GetDb(scope);

        var invoices = scope.ServiceProvider.GetRequiredService<IInvoiceApplicationService>();
        var created = await invoices.CreateDraftAsync(Draft(seeded), CancellationToken.None);
        var id = created.Value.Id;

        await BuildBlockedService(scope, seeded.TenantId)
            .IssueAsync(id, "NOCONSUME", CancellationToken.None);

        var invoice = await db.Invoices.AsNoTracking().SingleAsync(x => x.Id == id);
        Assert.Equal(InvoiceStatus.Draft, invoice.Status);
        Assert.Null(invoice.Number);
        Assert.Null(invoice.Series);

        // Ni siquiera debe existir la fila de secuencia: el rechazo ocurre antes
        // de tocar la numeracion, no despues de advancedla.
        var sequences = await db.InvoiceSequences.AsNoTracking()
            .Where(s => s.Series == "NOCONSUME").ToListAsync();
        Assert.Empty(sequences);
    }

    [Fact]
    public async Task El_rechazo_no_deja_registros_fiscales_ni_mensajes_de_outbox()
    {
        var seeded = _fixture.Seed();
        using var scope = _fixture.CreateScope(seeded.TenantId);
        var db = _fixture.GetDb(scope);

        var invoices = scope.ServiceProvider.GetRequiredService<IInvoiceApplicationService>();
        var created = await invoices.CreateDraftAsync(Draft(seeded), CancellationToken.None);

        await BuildBlockedService(scope, seeded.TenantId)
            .IssueAsync(created.Value.Id, "NOOUTBOX", CancellationToken.None);

        Assert.Empty(await db.FiscalRecords.AsNoTracking().ToListAsync());
        Assert.Empty(await db.OutboxMessages.AsNoTracking().ToListAsync());
    }

    [Fact]
    public async Task Con_el_motor_disponible_la_emision_crea_registro_y_outbox_en_la_misma_transaccion()
    {
        var seeded = _fixture.Seed();
        using var scope = _fixture.CreateScope(seeded.TenantId);
        var db = _fixture.GetDb(scope);

        var invoices = scope.ServiceProvider.GetRequiredService<IInvoiceApplicationService>();
        var created = await invoices.CreateDraftAsync(Draft(seeded), CancellationToken.None);

        // El fixture registra TestFiscalEngine, que si esta listo.
        var service = scope.ServiceProvider.GetRequiredService<IInvoiceIssuanceService>();
        var result = await service.IssueAsync(created.Value.Id, "OK", CancellationToken.None);

        Assert.True(result.IsSuccess, result.IsFailure ? result.Errors[0].Code : "");

        // Factura, registro fiscal y Outbox: los tres existen o ninguno.
        var record = await db.FiscalRecords.AsNoTracking()
            .SingleOrDefaultAsync(r => r.InvoiceId == created.Value.Id);
        Assert.NotNull(record);
        Assert.Equal(64, record.Hash.Length);

        var message = await db.OutboxMessages.AsNoTracking()
            .SingleOrDefaultAsync(m => m.AggregateId == record.Id.ToString());
        Assert.NotNull(message);
        Assert.Equal("Pending", message.Status);
        Assert.Equal(0, message.Attempts);
    }

    [Fact]
    public async Task La_cadena_avanza_con_cada_emision()
    {
        var seeded = _fixture.Seed();
        using var scope = _fixture.CreateScope(seeded.TenantId);
        var db = _fixture.GetDb(scope);

        var invoices = scope.ServiceProvider.GetRequiredService<IInvoiceApplicationService>();
        var service = scope.ServiceProvider.GetRequiredService<IInvoiceIssuanceService>();

        for (var i = 0; i < 2; i++)
        {
            var created = await invoices.CreateDraftAsync(Draft(seeded), CancellationToken.None);
            Assert.True(await service.IssueAsync(created.Value.Id, "CADENA", CancellationToken.None) is { IsSuccess: true });
        }

        // SQLite no admite ORDER BY sobre DateTimeOffset, asi que se ordena en memoria.
        var records = (await db.FiscalRecords.AsNoTracking()
            .Where(r => r.InvoiceId != Guid.Empty)
            .ToListAsync())
            .OrderBy(r => r.GeneratedAt)
            .ToList();

        Assert.Equal(2, records.Count);
        // El primero no encadena con nadie; el segundo encadena con el primero.
        Assert.Equal(string.Empty, records[0].PreviousHash);
        Assert.Equal(records[0].Hash, records[1].PreviousHash);
    }
}