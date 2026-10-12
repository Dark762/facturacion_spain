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
    public async Task<IReadOnlyList<OutboxMessage>> ClaimPendingAsync(
        int maxCount,
        TimeSpan lease,
        CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var limit = Math.Clamp(maxCount, 1, 500);

        // El filtro por estado se resuelve en la base de datos: es el campo
        // que define la cola. La ventana de candidatos se trae sin rastreo y
        // se ordena y acota en memoria.
        //
        // El motivo es que DateTimeOffset no es portable en SQLite: no traduce
        // la comparacion con una variable y tampoco admite ORDER BY. Ese
        // predicado estaba escrito dentro del arbol de consultas desde antes
        // de que existiera un worker que lo ejecutara, asi que nadie lo habia
        // llegado a ver fallar.
        var window = await _db.OutboxMessages.AsNoTracking()
            .IgnoreQueryFilters()
            .Where(x => x.Status == OutboxStatus.Pending || x.Status == OutboxStatus.Processing)
            .Take(Math.Clamp(limit * 4, 100, 2000))
            .ToListAsync(ct);

        var now2 = now;

        // Reclamable si nunca se Processes, o si su lease ya caduco. Un
        // mensaje en Processing con lease vigente NO se toca: significaria que
        // otro worker lo tiene entre manos ahora mismo.
        var candidates = window
            .Where(x => x.Status == OutboxStatus.Pending
                        ? x.NextAttemptAt is null || x.NextAttemptAt <= now2
                        : x.NextAttemptAt is not null && x.NextAttemptAt <= now2)
            .OrderBy(x => x.CreatedAt)
            .Take(limit)
            .Select(x => new { x.Id, x.Status })
            .ToList();

        var claimed = new List<OutboxMessage>(candidates.Count);

        foreach (var candidate in candidates)
        {
            // El Status se vuelve a comprobar dentro del propio UPDATE: si otro
            // worker se adelanto, la condicion falla y no afecta a filas.
            // Comparar contra el estado esperado es lo que hace la
            // reclamacion atomica sin necesidad de bloqueos.
            var affected = await _db.OutboxMessages
                .IgnoreQueryFilters()
                .Where(x => x.Id == candidate.Id && x.Status == candidate.Status)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.Status, OutboxStatus.Processing)
                    .SetProperty(x => x.NextAttemptAt, now + lease), ct);

            if (affected == 1)
            {
                var message = await _db.OutboxMessages.AsNoTracking()
                    .IgnoreQueryFilters()
                    .FirstAsync(x => x.Id == candidate.Id, ct);

                claimed.Add(message);
            }
        }

        return claimed;
    }

    public async Task MarkProcessedAsync(Guid id, DateTimeOffset processedAt, CancellationToken ct)
    {
        await _db.OutboxMessages
            .Where(x => x.Id == id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Status, OutboxStatus.Completed)
                .SetProperty(x => x.ProcessedAt, (DateTimeOffset?)processedAt)
                .SetProperty(x => x.NextAttemptAt, (DateTimeOffset?)null)
                .SetProperty(x => x.LastError, (string?)null), ct);
    }

    public async Task MarkFailedAsync(
        Guid id,
        DateTimeOffset now,
        string error,
        int maxAttempts,
        TimeSpan baseDelay,
        CancellationToken ct)
    {
        var current = await _db.OutboxMessages.AsNoTracking()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (current is null)
        {
            return;
        }

        var attempts = current.Attempts + 1;
        var exhausted = attempts >= Math.Max(1, maxAttempts);

        // Espera creciente: cada reintento cuesta mas que el anterior. Sin
        // esto, un fallo Systematico (AEAT caido, esquema invalido) se
        // convierte en un bucle que satura la cola y la base de datos.
        var delay = exhausted ? TimeSpan.Zero : baseDelay * Math.Pow(2, attempts - 1);

        await _db.OutboxMessages
            .Where(x => x.Id == id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Attempts, attempts)
                .SetProperty(x => x.Status,
                    exhausted ? OutboxStatus.Failed : OutboxStatus.Pending)
                .SetProperty(x => x.NextAttemptAt,
                    exhausted ? (DateTimeOffset?)null : now + delay)
                .SetProperty(x => x.LastError, (string?)error), ct);
    }
}