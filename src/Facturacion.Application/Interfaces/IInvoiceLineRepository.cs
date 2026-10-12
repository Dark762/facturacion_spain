using Facturacion.Domain.Entities;
namespace Facturacion.Application.Interfaces;
public interface IInvoiceLineRepository
{
    Task<InvoiceLine?> GetByIdAsync(Guid id, CancellationToken ct);
    Task AddAsync(InvoiceLine entity, CancellationToken ct);
}
