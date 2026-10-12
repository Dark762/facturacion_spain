using Facturacion.Domain.Entities;
namespace Facturacion.Application.Interfaces;
public interface ICompanyRepository
{
    Task<IReadOnlyList<Company>> GetAllAsync(CancellationToken ct);
    Task<Company?> GetByIdAsync(Guid id, CancellationToken ct);
    Task AddAsync(Company entity, CancellationToken ct);
}
