using Facturacion.Application.Interfaces;
using Facturacion.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Facturacion.Infrastructure.Persistence.Repositories;

public sealed class OutboxMessageRepository : IOutboxMessageRepository
{
    private readonly BillingDbContext _db;
    public OutboxMessageRepository(BillingDbContext db) => _db = db;

    public Task<OutboxMessage?> GetByIdAsync(Guid id, CancellationToken ct) =>
        _db.OutboxMessages.FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task AddAsync(OutboxMessage entity, CancellationToken ct) =>
        await _db.OutboxMessages.AddAsync(entity, ct);

    // Solo el tenant actual. Un worker debe establecer su contexto antes de llamar.
    public async Task<IReadOnlyList<OutboxMessage>> GetPendingAsync(int maxCount, CancellationToken ct)
    {
        // Se evalua fuera del arbol de consultas: no todos los proveedores
        // traducen DateTimeOffset.UtcNow a SQL.
        var now = DateTimeOffset.UtcNow;

        return await _db.OutboxMessages.AsNoTracking()
            .Where(x => x.Status == OutboxStatus.Pending && (x.NextAttemptAt == null || x.NextAttemptAt <= now))
            .OrderBy(x => x.CreatedAt)
            .Take(Math.Clamp(maxCount, 1, 500)).ToListAsync(ct);
    }

    /// <summary>
    /// Reclama mensajes de forma atomica.
    ///
    /// Un worker recorre todos los tenants, asi que ignora el filtro de
    /// consulta: el aislamiento lo aplica despues el procesador, que abre un
    /// ambito nuevo por tenant. Ignorarlo aqui es deliberado y no equivale a
    /// saltarselo en el negocio.
    ///
    /// La reclamacion se hace con ExecuteUpdate y se comprueba el numero de
    /// filas afectadas: eso decide en la base de datos, no en memoria, que el
    /// mensaje es nuestro. Dos workers concurrentes no pueden salir los dos
    /// con el mismo mensaje.
    /// </summary>
    public Task<IReadOnlyList<OutboxMessage>> ClaimPendingAsync(
        int maxCount,
        TimeSpan lease,
        CancellationToken ct) => ClaimPendingAsync(maxCount, lease, null, ct);

    /// <summary>
    /// Reclama mensajes de forma atomica.
    ///
    /// Un worker recorre todos los tenants, asi que ignora el filtro de
    /// consulta: el aislamiento lo aplica despues el procesador, que abre un
    /// ambito nuevo por tenant. Ignorarlo aqui es deliberado y no equivale a
    /// saltarselo en el negocio.
    ///
    /// La reclamacion se hace con ExecuteUpdate y se comprueba el numero de
    /// filas afectadas: eso decide en la base de datos, no en memoria, que el
    /// mensaje es nuestro. Dos workers concurrentes no pueden salir los dos
    /// con el mismo mensaje gracias a la comprobacion concurrente de Status,
    /// ClaimToken y NextAttemptAt.
    /// </summary>
    public async Task<IReadOnlyList<OutboxMessage>> ClaimPendingAsync(
        int maxCount,
        TimeSpan lease,
        string? type,
        CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var limit = Math.Clamp(maxCount, 1, 500);

        var query = _db.OutboxMessages.AsNoTracking().IgnoreQueryFilters();
        if (!string.IsNullOrWhiteSpace(type))
        {
            query = query.Where(x => x.Type == type);
        }

        var window = await query
            .Where(x => x.Status == OutboxStatus.Pending || x.Status == OutboxStatus.Processing)
            .Take(Math.Clamp(limit * 4, 100, 2000))
            .ToListAsync(ct);

        var now2 = now;

        var candidates = window
            .Where(x => x.Status == OutboxStatus.Pending
                        ? x.NextAttemptAt is null || x.NextAttemptAt <= now2
                        : x.NextAttemptAt is not null && x.NextAttemptAt <= now2)
            .OrderBy(x => x.CreatedAt)
            .ThenBy(x => x.Id)
            .Take(limit)
            .Select(x => new { x.Id, x.Status, x.ClaimToken })
            .ToList();

        var claimed = new List<OutboxMessage>(candidates.Count);

        foreach (var candidate in candidates)
        {
            var token = Guid.NewGuid().ToString("N");
            var candidateId = candidate.Id;
            var candidateStatus = candidate.Status;
            var candidateToken = candidate.ClaimToken;

            var updateQuery = _db.OutboxMessages
                .IgnoreQueryFilters()
                .Where(x => x.Id == candidateId && x.Status == candidateStatus);

            if (candidateStatus == OutboxStatus.Processing)
            {
                // Para reclamar un lease vencido, el ClaimToken debe coincidir con el
                // observado: si otro worker se adelanto, su token ya es diferente.
                updateQuery = updateQuery.Where(x => x.ClaimToken == candidateToken);
            }

            var affected = await updateQuery
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.Status, OutboxStatus.Processing)
                    .SetProperty(x => x.NextAttemptAt, now + lease)
                    .SetProperty(x => x.ClaimToken, token), ct);

            if (affected == 1)
            {
                var message = await _db.OutboxMessages.AsNoTracking()
                    .IgnoreQueryFilters()
                    .FirstAsync(x => x.Id == candidateId, ct);

                claimed.Add(message);
            }
        }

        return claimed;
    }

    public Task<bool> MarkProcessedAsync(Guid id, DateTimeOffset processedAt, CancellationToken ct) =>
        MarkProcessedAsync(id, null, processedAt, ct);

    public async Task<bool> MarkProcessedAsync(
        Guid id,
        string? claimToken,
        DateTimeOffset processedAt,
        CancellationToken ct)
    {
        var query = _db.OutboxMessages.IgnoreQueryFilters().Where(x => x.Id == id);
        if (!string.IsNullOrWhiteSpace(claimToken))
        {
            query = query.Where(x => x.Status == OutboxStatus.Processing && x.ClaimToken == claimToken);
        }

        var affected = await query
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Status, OutboxStatus.Completed)
                .SetProperty(x => x.ProcessedAt, (DateTimeOffset?)processedAt)
                .SetProperty(x => x.NextAttemptAt, (DateTimeOffset?)null)
                .SetProperty(x => x.ClaimToken, (string?)null)
                .SetProperty(x => x.LastError, (string?)null), ct);

        return affected > 0;
    }

    public Task<bool> MarkFailedAsync(
        Guid id,
        DateTimeOffset now,
        string error,
        int maxAttempts,
        TimeSpan baseDelay,
        CancellationToken ct) => MarkFailedAsync(id, null, now, error, maxAttempts, baseDelay, ct);

    public async Task<bool> MarkFailedAsync(
        Guid id,
        string? claimToken,
        DateTimeOffset now,
        string error,
        int maxAttempts,
        TimeSpan baseDelay,
        CancellationToken ct)
    {
        var currentQuery = _db.OutboxMessages.AsNoTracking().IgnoreQueryFilters().Where(x => x.Id == id);
        if (!string.IsNullOrWhiteSpace(claimToken))
        {
            currentQuery = currentQuery.Where(x => x.ClaimToken == claimToken);
        }

        var current = await currentQuery.FirstOrDefaultAsync(ct);

        if (current is null)
        {
            return false;
        }

        var attempts = current.Attempts + 1;
        var exhausted = attempts >= Math.Max(1, maxAttempts);

        // Espera creciente: cada reintento cuesta mas que el anterior. Sin
        // esto, un fallo Systematico (AEAT caido, esquema invalido) se
        // convierte en un bucle que satura la cola y la base de datos.
        var delay = exhausted ? TimeSpan.Zero : baseDelay * Math.Pow(2, attempts - 1);

        var updateQuery = _db.OutboxMessages.IgnoreQueryFilters().Where(x => x.Id == id);
        if (!string.IsNullOrWhiteSpace(claimToken))
        {
            updateQuery = updateQuery.Where(x => x.Status == OutboxStatus.Processing && x.ClaimToken == claimToken);
        }

        var affected = await updateQuery
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Attempts, attempts)
                .SetProperty(x => x.Status,
                    exhausted ? OutboxStatus.Failed : OutboxStatus.Pending)
                .SetProperty(x => x.NextAttemptAt,
                    exhausted ? (DateTimeOffset?)null : now + delay)
                .SetProperty(x => x.ClaimToken, (string?)null)
                .SetProperty(x => x.LastError, (string?)error), ct);

        return affected > 0;
    }
}