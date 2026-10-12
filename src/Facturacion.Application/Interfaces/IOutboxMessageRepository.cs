using Facturacion.Domain.Entities;
namespace Facturacion.Application.Interfaces;

/// <summary>
/// Estados del outbox.
///
/// Antes eran cadenas sueltas escritas en varios sitios, y solo se usaba
/// "Pending": no habia forma de distinguir un mensaje en curso de uno que
/// fallo, ni de un worker que murio dejandolo a medias.
/// </summary>
public static class OutboxStatus
{
    /// <summary>En la cola, todavia sin reclamar.</summary>
    public const string Pending = "Pending";

    /// <summary>Reclamado por un worker mediante un lease con caducidad.</summary>
    public const string Processing = "Processing";

    /// <summary>Procesado con exito. Terminal.</summary>
    public const string Completed = "Completed";

    /// <summary>Agotados los intentos. Terminal: requiere intervencion.</summary>
    public const string Failed = "Failed";

    public static bool IsTerminal(string status) => status is Completed or Failed;
}

public interface IOutboxMessageRepository
{
    Task<IReadOnlyList<OutboxMessage>> GetPendingAsync(int maxCount, CancellationToken ct);
    Task<OutboxMessage?> GetByIdAsync(Guid id, CancellationToken ct);
    Task AddAsync(OutboxMessage entity, CancellationToken ct);

    /// <summary>
    /// Reclama mensajes de forma atomica y devuelve los que este worker ha
    /// tomado. Reclamar (y no solo leer) es lo que permite ejecutar varios
    /// workers a la vez sin procesar dos veces el mismo mensaje.
    ///
    /// El lease es una caducidad: si el worker cae a mitad, el mensaje vuelve
    /// a estar disponible solo cuando expire, en lugar de quedarse bloqueado
    /// para siempre.
    /// </summary>
    Task<IReadOnlyList<OutboxMessage>> ClaimPendingAsync(
        int maxCount,
        TimeSpan lease,
        CancellationToken ct);

    /// <summary>
    /// Reclama mensajes de un tipo específico de forma atómica.
    /// Permite que cada procesador solo reclame los eventos que sabe atender.
    /// </summary>
    Task<IReadOnlyList<OutboxMessage>> ClaimPendingAsync(
        int maxCount,
        TimeSpan lease,
        string? type,
        CancellationToken ct);

    Task<bool> MarkProcessedAsync(Guid id, DateTimeOffset processedAt, CancellationToken ct);

    /// <summary>
    /// Marca un mensaje como completado verificando que el worker conserva la reclamación activa.
    /// Un worker con lease caducado que fue reclamado por otro worker será rechazado (devuelve false).
    /// </summary>
    Task<bool> MarkProcessedAsync(Guid id, string? claimToken, DateTimeOffset processedAt, CancellationToken ct);

    /// <summary>
    /// Registra un fallo y reprograma el reintento con espera creciente. Al
    /// agotar los intentos el mensaje queda en Failed y no vuelve a la cola.
    /// </summary>
    Task<bool> MarkFailedAsync(
        Guid id,
        DateTimeOffset now,
        string error,
        int maxAttempts,
        TimeSpan baseDelay,
        CancellationToken ct);

    /// <summary>
    /// Registra un fallo verificando que el worker conserva la reclamación activa.
    /// </summary>
    Task<bool> MarkFailedAsync(
        Guid id,
        string? claimToken,
        DateTimeOffset now,
        string error,
        int maxAttempts,
        TimeSpan baseDelay,
        CancellationToken ct);
}