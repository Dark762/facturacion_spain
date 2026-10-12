namespace Facturacion.Application.Interfaces;
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
    Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken ct = default);

    /// <summary>
    /// Descarta el estado pendiente del rastreador de cambios. Necesario tras un
    /// rollback antes de reintentar: las entidades siguen marcadas como
    /// modificadas o anadidas y un nuevo intento volveria a escribirlas.
    /// </summary>
    Task ResetAsync(CancellationToken ct = default);
}
public interface IUnitOfWorkTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken ct = default);
    Task RollbackAsync(CancellationToken ct = default);
}
