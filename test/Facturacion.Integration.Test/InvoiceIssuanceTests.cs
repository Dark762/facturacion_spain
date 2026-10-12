using Facturacion.Application.Common.Results;
using Facturacion.Application.DTOs.Invoices;
using Facturacion.Application.Interfaces;
using Facturacion.Application.Services;
using Facturacion.Domain.Enums;
using Facturacion.Infrastructure.Persistence;
using Facturacion.Integration.Test.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Facturacion.Integration.Test;

/// <summary>
/// Emision con numeracion definitiva. Lo critico es que la numeracion sea
/// correlativa sin huecos bajo concurrencia y que reintentar una emision ya
/// realizada no duplique numeros.
/// </summary>
public sealed class InvoiceIssuanceTests : IClassFixture<BillingDatabaseFixture>
{
    private readonly BillingDatabaseFixture _fixture;
    public InvoiceIssuanceTests(BillingDatabaseFixture fixture) => _fixture = fixture;

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

    [Fact]
    public async Task Issuing_assigns_a_correlative_number_and_marks_the_invoice()
    {
        var seeded = _fixture.Seed();
        using var scope = _fixture.CreateScope(seeded.TenantId);

        var id = await CreateDraftAsync(scope, seeded);
        var issuance = scope.ServiceProvider.GetRequiredService<IInvoiceIssuanceService>();

        var result = await issuance.IssueAsync(id, "2026", CancellationToken.None);

        Assert.True(result.IsSuccess, result.IsFailure ? result.Errors[0].Code : "");
        Assert.Equal("2026/1", result.Value.InvoiceNumber);

        var db = _fixture.GetDb(scope);
        var stored = await db.Invoices.AsNoTracking().SingleAsync(x => x.Id == id);
        Assert.Equal("2026", stored.Series);
        Assert.Equal("1", stored.Number);
        Assert.Equal(InvoiceStatus.Issued, stored.Status);
        Assert.NotNull(stored.IssuedAt);
    }

    [Fact]
    public async Task Numbers_are_correlative_and_never_reused()
    {
        var seeded = _fixture.Seed();
        using var scope = _fixture.CreateScope(seeded.TenantId);

        var issuance = scope.ServiceProvider.GetRequiredService<IInvoiceIssuanceService>();
        var issued = new List<string>();

        for (var i = 0; i < 3; i++)
        {
            var id = await CreateDraftAsync(scope, seeded);
            var r = await issuance.IssueAsync(id, "A", CancellationToken.None);
            Assert.True(r.IsSuccess);
            issued.Add(r.Value.InvoiceNumber);
        }

        Assert.Equal(new[] { "A/1", "A/2", "A/3" }, issued);
    }

    [Fact]
    public async Task Re_issuing_returns_the_same_number_without_advancing_the_sequence()
    {
        var seeded = _fixture.Seed();
        using var scope = _fixture.CreateScope(seeded.TenantId);

        var id = await CreateDraftAsync(scope, seeded);
        var issuance = scope.ServiceProvider.GetRequiredService<IInvoiceIssuanceService>();

        var first = await issuance.IssueAsync(id, "B", CancellationToken.None);
        var second = await issuance.IssueAsync(id, "B", CancellationToken.None);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.Equal(first.Value.InvoiceNumber, second.Value.InvoiceNumber);

        // La secuencia no debe haber avanzado por el reintento.
        var db = _fixture.GetDb(scope);
        var sequence = await db.InvoiceSequences.AsNoTracking()
            .SingleAsync(x => x.Series == "B");
        Assert.Equal(2, sequence.NextNumber);
    }

    [Fact]
    public async Task A_different_series_has_its_own_independent_numbering()
    {
        var seeded = _fixture.Seed();
        using var scope = _fixture.CreateScope(seeded.TenantId);
        var issuance = scope.ServiceProvider.GetRequiredService<IInvoiceIssuanceService>();

        var a = await CreateDraftAsync(scope, seeded);
        var b = await CreateDraftAsync(scope, seeded);

        var ra = await issuance.IssueAsync(a, "SERIE-A", CancellationToken.None);
        var rb = await issuance.IssueAsync(b, "SERIE-B", CancellationToken.None);

        Assert.Equal("SERIE-A/1", ra.Value.InvoiceNumber);
        Assert.Equal("SERIE-B/1", rb.Value.InvoiceNumber);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task An_empty_series_is_rejected(string? series)
    {
        var seeded = _fixture.Seed();
        using var scope = _fixture.CreateScope(seeded.TenantId);
        var id = await CreateDraftAsync(scope, seeded);

        var issuance = scope.ServiceProvider.GetRequiredService<IInvoiceIssuanceService>();
        var result = await issuance.IssueAsync(id, series!, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("SERIES_REQUIRED", result.Errors[0].Code);
    }

    [Fact]
    public async Task A_series_longer_than_the_column_is_rejected()
    {
        var seeded = _fixture.Seed();
        using var scope = _fixture.CreateScope(seeded.TenantId);
        var id = await CreateDraftAsync(scope, seeded);

        var issuance = scope.ServiceProvider.GetRequiredService<IInvoiceIssuanceService>();
        var result = await issuance.IssueAsync(id, new string('X', 33), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("SERIES_TOO_LONG", result.Errors[0].Code);
    }

    [Fact]
    public async Task An_unknown_invoice_is_not_found()
    {
        var seeded = _fixture.Seed();
        using var scope = _fixture.CreateScope(seeded.TenantId);
        var issuance = scope.ServiceProvider.GetRequiredService<IInvoiceIssuanceService>();

        var result = await issuance.IssueAsync(Guid.NewGuid(), "X", CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("RESOURCE_NOT_FOUND", result.Errors[0].Code);
    }

    [Fact]
    public async Task An_invoice_from_another_tenant_is_not_found()
    {
        var mine = _fixture.Seed();
        var theirs = _fixture.Seed();

        Guid foreignId;
        using (var theirsScope = _fixture.CreateScope(theirs.TenantId))
            foreignId = await CreateDraftAsync(theirsScope, theirs);

        using var scope = _fixture.CreateScope(mine.TenantId);
        var issuance = scope.ServiceProvider.GetRequiredService<IInvoiceIssuanceService>();

        var result = await issuance.IssueAsync(foreignId, "X", CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("RESOURCE_NOT_FOUND", result.Errors[0].Code);
    }

    [Fact]
    public async Task Denied_authorization_blocks_issuance()
    {
        var seeded = _fixture.Seed();
        using var scope = _fixture.CreateScope(seeded.TenantId);

        var id = await CreateDraftAsync(scope, seeded);

        var authorization = scope.ServiceProvider
            .GetRequiredService<BillingDatabaseFixture.StubCompanyAuthorizationService>();
        authorization.Allow = false;

        var issuance = scope.ServiceProvider.GetRequiredService<IInvoiceIssuanceService>();
        var result = await issuance.IssueAsync(id, "X", CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("ACCESS_DENIED", result.Errors[0].Code);
        Assert.Equal("invoices.issue", authorization.LastPermission);

        // No debe haberse creado ninguna secuencia.
        var db = _fixture.GetDb(scope);
        Assert.Empty(await db.InvoiceSequences.AsNoTracking().Where(x => x.Series == "X").ToListAsync());
    }

    [Fact(Skip = "SQLite serializa toda la escritura y no admite SELECT ... FOR UPDATE, " +
             "por lo que no puede validar el bloqueo de fila. La garantia real se " +
             "verifica contra MariaDB/MySQL con el bloqueo pesimista activo.")]
    public async Task Concurrency_does_not_produce_duplicate_numbers()
    {
        var seeded = _fixture.Seed();

        const int drafts = 8;
        var ids = new List<Guid>();
        using (var scope = _fixture.CreateScope(seeded.TenantId))
            for (var i = 0; i < drafts; i++) ids.Add(await CreateDraftAsync(scope, seeded));

        var numbers = new System.Collections.Concurrent.ConcurrentBag<string>();

        // Diez tareas compiten por la misma serie. Solo las 8 emisiones validas
        // deben conseguir numero, y ningun numero puede repetirse.
        await Task.WhenAll(ids.Select(id => Task.Run(async () =>
        {
            using var scope = _fixture.CreateScope(seeded.TenantId);
            var issuance = scope.ServiceProvider.GetRequiredService<IInvoiceIssuanceService>();
            var r = await issuance.IssueAsync(id, "CONC", CancellationToken.None);
            if (r.IsSuccess) numbers.Add(r.Value.InvoiceNumber);
        })));

        var distinct = numbers.Distinct().ToList();
        Assert.Equal(numbers.Count, distinct.Count);
        Assert.Equal(drafts, numbers.Count);
    }
}