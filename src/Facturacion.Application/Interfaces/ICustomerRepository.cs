using Facturacion.Domain.Entities;
namespace Facturacion.Application.Interfaces;
public interface ICustomerRepository
{
    Task<IReadOnlyList<Customer>> GetByCompanyAsync(Guid companyId, CancellationToken ct);
    Task<Customer?> GetByIdAsync(Guid id, CancellationToken ct);
    Task AddAsync(Customer entity, CancellationToken ct);
}
