using Facturacion.Domain.Entities;
namespace Facturacion.Application.Interfaces;
public interface IFiscalChainRepository
{
    Task<FiscalChain?> GetByIdAsync(Guid id, CancellationToken ct);

    /// <summary>Cadena vigente de una empresa emisora. Se amplia de forma compatible.</summary>
    Task<FiscalChain?> GetByCompanyAsync(Guid tenantId, Guid companyId, CancellationToken ct);
    Task AddAsync(FiscalChain entity, CancellationToken ct);
}
