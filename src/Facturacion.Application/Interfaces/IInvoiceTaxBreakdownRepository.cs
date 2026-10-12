using Facturacion.Domain.Entities;
namespace Facturacion.Application.Interfaces;
public interface IInvoiceTaxBreakdownRepository
{
    Task<InvoiceTaxBreakdown?> GetByIdAsync(Guid id, CancellationToken ct);
    Task AddAsync(InvoiceTaxBreakdown entity, CancellationToken ct);
}
