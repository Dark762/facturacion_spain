using Facturacion.Application.Interfaces;
using Facturacion.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Facturacion.Infrastructure.Persistence.Repositories;

public sealed class InvoiceSequenceRepository : IInvoiceSequenceRepository
{
    private readonly BillingDbContext _db;
    private readonly ITenantContext _tenant;

    public InvoiceSequenceRepository(BillingDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public Task<InvoiceSequence?> GetByIdAsync(Guid id, CancellationToken ct) =>
        _db.InvoiceSequences.FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task AddAsync(InvoiceSequence entity, CancellationToken ct) =>
        await _db.InvoiceSequences.AddAsync(entity, ct);

    public Task<InvoiceSequence?> GetBySeriesAsync(Guid companyId, string series, int fiscalYear, CancellationToken ct) =>
        _db.InvoiceSequences.FirstOrDefaultAsync(
            x => x.CompanyId == companyId && x.Series == series && x.FiscalYear == fiscalYear, ct);

    public async Task<InvoiceSequence?> LockBySeriesAsync(
        Guid companyId, string series, int fiscalYear, CancellationToken ct)
    {
        // El bloqueo pesimista garantiza que dos emisiones concurrentes no toman
        // el mismo numero. La sentencia es SQL crudo, por lo que el filtro global
        // de tenant NO se aplica: hay que incluir TenantId de forma explicita.
        //
        // Debe invocarse dentro de una transaccion; si no, el bloqueo se libera al
        // terminar la sentencia y no protege nada.
        var lockSql = BuildLockSql(
            _db.Database.ProviderName, _tenant.TenantId, companyId, series, fiscalYear);

        // SQLite (usado solo en pruebas) no admite bloqueo de fila: se serializa
        // con la transaccion y el indice unico, no con SELECT ... FOR UPDATE.
        if (lockSql is not null)
            await _db.Database.ExecuteSqlRawAsync(lockSql, ct);

        return await _db.InvoiceSequences.FirstOrDefaultAsync(
            x => x.CompanyId == companyId && x.Series == series && x.FiscalYear == fiscalYear, ct);
    }

    private static string? BuildLockSql(
        string? providerName, Guid tenantId, Guid companyId, string series, int fiscalYear)
    {
        var provider = providerName ?? string.Empty;
        var quotedSeries = series.Replace("'", "''");

        var isSqlServer = provider.Contains("SqlServer", StringComparison.OrdinalIgnoreCase);
        var isPostgres = provider.Contains("Npgsql", StringComparison.OrdinalIgnoreCase);
        var supportsRowLock = isSqlServer || isPostgres ||
                              provider.Contains("MySql", StringComparison.OrdinalIgnoreCase);

        // SQLite e InMemory no lo soportan; se devuelven como null.
        if (!supportsRowLock) return null;

        var table = isPostgres ? "\"InvoiceSequences\"" : "`InvoiceSequences`";
        var lockClause = isSqlServer ? "WITH (UPDLOCK, ROWLOCK)" : "FOR UPDATE";

        return $"SELECT 1 FROM {table} " +
               $"WHERE TenantId = '{tenantId:D}' " +
               $"AND CompanyId = '{companyId:D}' " +
               $"AND Series = '{quotedSeries}' " +
               $"AND FiscalYear = {fiscalYear} {lockClause};";
    }
}