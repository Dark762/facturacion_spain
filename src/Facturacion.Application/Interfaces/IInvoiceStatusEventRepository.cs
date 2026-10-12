using Facturacion.Domain.Entities;
namespace Facturacion.Application.Interfaces;
public interface IInvoiceStatusEventRepository
{
    Task<InvoiceStatusEvent?> GetByIdAsync(Guid id, CancellationToken ct);
    Task AddAsync(InvoiceStatusEvent entity, CancellationToken ct);
}
