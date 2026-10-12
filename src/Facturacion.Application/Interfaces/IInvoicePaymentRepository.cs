using Facturacion.Domain.Entities;
namespace Facturacion.Application.Interfaces;
public interface IInvoicePaymentRepository
{
    Task<InvoicePayment?> GetByIdAsync(Guid id, CancellationToken ct);
    Task AddAsync(InvoicePayment entity, CancellationToken ct);
}
