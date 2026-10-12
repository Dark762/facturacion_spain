using Facturacion.Domain.Entities;
namespace Facturacion.Application.Interfaces;
public interface IFiscalChainRepository
{
    Task<FiscalChain?> GetByIdAsync(Guid id, CancellationToken ct);
    Task AddAsync(FiscalChain entity, CancellationToken ct);
}
