using Facturacion.Application.Common.Results;
using Facturacion.Application.DTOs.Invoices;
using Facturacion.Application.Interfaces;
using Facturacion.Infrastructure.Persistence;
using Facturacion.Integration.Test.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Facturacion.Integration.Test;

/// <summary>
/// Correcciones de emision.
///   A. Las rutas de idempotencia ya no devuelven Guid.Empty.
///   B. Una sola marca temporal para toda la emision.
///   C. Numeracion correlativa y avance de la cadena.
/// </summary>
public sealed class InvoiceIssuanceCorrectionsTests : IClassFixture<BillingDatabaseFixture>
{
    private readonly BillingDatabaseFixture _fixture;

    public InvoiceIssuanceCorrectionsTests(BillingDatabaseFixture fixture) => _fixture = fixture;

    private static CreateInvoiceRequest Draft(BillingDatabaseFixture.SeededCompany s) => new(
        s.CompanyId, s.CustomerId, new DateOnly(2026, 3, 15), "EUR",
        new[] { new CreateInvoiceLineRequest("Consultoria", 2m, 50m, 21m) });

    private async Task<Guid> CreateDraftAsync(IServiceScope scope, BillingDatabaseFixture.SeededCompany s)
    {
        var invoices = scope.ServiceProvider.GetRequiredService<IInvoiceApplicationService>();
        var result = await invoices.CreateDraftAsync(Draft(s), CancellationToken.None);
        Assert.True(result.IsSuccess, result.IsFailure ? result.Errors[0].Code : "");
        return result.Value.Id;
    }

    // ---------- A. Identificador fiscal real ----------

    [Fact]
    public async Task Reemitir_devuelve_el_identificador_fiscal_real_y_no_uno_ficticio()
    {
        var seeded = _fixture.Seed();
        using var scope = _fixture.CreateScope(seeded.TenantId);
        var db = _fixture.GetDb(scope);

        var id = await CreateDraftAsync(scope, seeded);
        var issuance = scope.ServiceProvider.GetRequiredService<IInvoiceIssuanceService>();

        var first = await issuance.IssueAsync(id, "REAL", CancellationToken.None);
        Assert.True(first.IsSuccess);
        Assert.NotEqual(Guid.Empty, first.Value.FiscalRecordId);

        // Ruta de idempotencia: antes devolvia Guid.Empty sin consultar la base.
        var second = await issuance.IssueAsync(id, "REAL", CancellationToken.None);

        Assert.True(second.IsSuccess);
        Assert.Equal(first.Value.FiscalRecordId, second.Value.FiscalRecordId);
        Assert.NotEqual(Guid.Empty, second.Value.FiscalRecordId);

        var stored = await db.FiscalRecords.AsNoTracking()
            .SingleOrDefaultAsync(r => r.Id == second.Value.FiscalRecordId);
        Assert.NotNull(stored);
        Assert.Equal(id, stored.InvoiceId);
    }

    [Fact]
    public async Task Una_factura_emitida_sin_registro_fiscal_se_rechaza_en_lugar_de_fingir_exito()
    {
        var seeded = _fixture.Seed();
        using var scope = _fixture.CreateScope(seeded.TenantId);
        var db = _fixture.GetDb(scope);

        var id = await CreateDraftAsync(scope, seeded);
        var issuance = scope.ServiceProvider.GetRequiredService<IInvoiceIssuanceService>();

        await issuance.IssueAsync(id, "HUERF", CancellationToken.None);

        // Se borra el registro fiscal: inconsistencia real.
        await db.FiscalRecords.AsNoTracking().Where(r => r.InvoiceId == id).ExecuteDeleteAsync();

        var again = await issuance.IssueAsync(id, "HUERF", CancellationToken.None);

        Assert.True(again.IsFailure);
        Assert.Equal("FISCAL_RECORD_MISSING", again.Errors[0].Code);
    }

    // ---------- B. Marca temporal unica ----------

    [Fact]
    public async Task La_marca_temporal_del_registro_fiscal_coincide_con_la_de_la_factura()
    {
        var seeded = _fixture.Seed();
        using var scope = _fixture.CreateScope(seeded.TenantId);
        var db = _fixture.GetDb(scope);

        var id = await CreateDraftAsync(scope, seeded);
        await scope.ServiceProvider.GetRequiredService<IInvoiceIssuanceService>()
            .IssueAsync(id, "HORA", CancellationToken.None);

        var invoice = await db.Invoices.AsNoTracking().SingleAsync(x => x.Id == id);
        var record = await db.FiscalRecords.AsNoTracking().SingleAsync(r => r.InvoiceId == id);

        // Antes IssuedAt y GeneratedAt venian de dos llamadas a UtcNow y la
        // huella de Now: tres instantes distintos.
        Assert.Equal(invoice.IssuedAt!.Value.UtcDateTime, record.GeneratedAt.UtcDateTime);
    }

    [Fact]
    public async Task La_cadena_y_el_outbox_usan_la_misma_marca_temporal()
    {
        var seeded = _fixture.Seed();
        using var scope = _fixture.CreateScope(seeded.TenantId);
        var db = _fixture.GetDb(scope);

        var id = await CreateDraftAsync(scope, seeded);
        await scope.ServiceProvider.GetRequiredService<IInvoiceIssuanceService>()
            .IssueAsync(id, "HORA2", CancellationToken.None);

        var record = await db.FiscalRecords.AsNoTracking().SingleAsync(r => r.InvoiceId == id);
        var message = await db.OutboxMessages.AsNoTracking()
            .SingleAsync(m => m.AggregateId == record.Id.ToString());

        Assert.Equal(record.GeneratedAt.UtcDateTime, message.CreatedAt.UtcDateTime);
    }

    // ---------- C. Numeracion y cadena ----------

    /// <summary>
    /// Emite varias facturas correlativamente y comprueba que no se repiten
    /// numeros ni se dejan huecos.
    ///
    /// NO es una prueba de concurrencia REAL. SQLite en memoria no admite
    /// escrituras paralelas (falla con "database is locked"), de modo que la
    /// concurrencia autentica queda PENDIENTE POR INFRAESTRUCTURA: requiere un
    /// servidor real (MariaDB, PostgreSQL o SQL Server) montado en el proyecto
    /// de pruebas.
    /// </summary>
    [Fact]
    public async Task Emissions_correlativas_no_repeten_numero_ni_dejan_huecos()
    {
        var seeded = _fixture.Seed();
        var ids = new List<Guid>();

        for (var i = 0; i < 4; i++)
        {
            using var scope = _fixture.CreateScope(seeded.TenantId);
            ids.Add(await CreateDraftAsync(scope, seeded));
        }

        var numbers = new List<string>();
        foreach (var invoiceId in ids)
        {
            using var scope = _fixture.CreateScope(seeded.TenantId);
            var result = await scope.ServiceProvider.GetRequiredService<IInvoiceIssuanceService>()
                .IssueAsync(invoiceId, "CONC", CancellationToken.None);
            Assert.True(result.IsSuccess, result.IsFailure ? result.Errors[0].Code : "");
            numbers.Add(result.Value.InvoiceNumber);
        }

        Assert.Equal(numbers.Count, numbers.Distinct().Count());

        var ordered = numbers.Select(n => int.Parse(n.Split('/')[1])).OrderBy(n => n).ToList();
        Assert.Equal(Enumerable.Range(1, ordered.Count), ordered);
    }

    [Fact]
    public async Task La_cadena_encadena_un_registro_con_el_anterior_sin_saltos()
    {
        var seeded = _fixture.Seed();
        using var scope = _fixture.CreateScope(seeded.TenantId);
        var db = _fixture.GetDb(scope);

        var ids = new List<Guid>();
        for (var i = 0; i < 3; i++)
            ids.Add(await CreateDraftAsync(scope, seeded));

        var issuance = scope.ServiceProvider.GetRequiredService<IInvoiceIssuanceService>();
        foreach (var invoiceId in ids)
        {
            var result = await issuance.IssueAsync(invoiceId, "CADENA2", CancellationToken.None);
            Assert.True(result.IsSuccess, result.IsFailure ? result.Errors[0].Code : "");
        }

        var records = (await db.FiscalRecords.AsNoTracking().ToListAsync())
            .Where(r => ids.Contains(r.InvoiceId))
            .OrderBy(r => r.GeneratedAt)
            .ToList();

        Assert.Equal(3, records.Count);

        // El primero no encadena con nadie; cada siguiente con el anterior.
        Assert.Equal(string.Empty, records[0].PreviousHash);
        for (var i = 1; i < records.Count; i++)
            Assert.Equal(records[i - 1].Hash, records[i].PreviousHash);

        var chain = await db.FiscalChains.AsNoTracking().SingleAsync();
        Assert.Equal(records[^1].Id, chain.LastRecordId);
        Assert.Equal(records[^1].Hash, chain.LastHash);
    }
}