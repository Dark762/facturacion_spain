using Facturacion.Application.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;

namespace Facturacion.Infrastructure.Persistence;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly BillingDbContext _db;
    public UnitOfWork(BillingDbContext db) => _db = db;

    public Task<int> SaveChangesAsync(CancellationToken ct = default) =>
        _db.SaveChangesAsync(ct);

    public async Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken ct = default) =>
        new EfTransaction(await _db.Database.BeginTransactionAsync(ct));

    private sealed class EfTransaction : IUnitOfWorkTransaction
    {
        private readonly IDbContextTransaction _tx;
        public EfTransaction(IDbContextTransaction tx) => _tx = tx;
        public Task CommitAsync(CancellationToken ct = default) => _tx.CommitAsync(ct);
        public Task RollbackAsync(CancellationToken ct = default) => _tx.RollbackAsync(ct);
        public ValueTask DisposeAsync() => _tx.DisposeAsync();
    }
}
