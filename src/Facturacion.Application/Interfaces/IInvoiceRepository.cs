using Facturacion.Domain.Entities;
namespace Facturacion.Application.Interfaces;
public interface IInvoiceRepository
{
    Task<Invoice?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Company?> GetCompanyAsync(Guid companyId, CancellationToken ct);
    Task<Customer?> GetCustomerAsync(Guid companyId, Guid customerId, CancellationToken ct);
    Task AddAsync(Invoice invoice, CancellationToken ct);
}
