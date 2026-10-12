using Facturacion.Domain.Entities;
namespace Facturacion.Application.Interfaces;
public interface IInvoiceSequenceRepository
{
    Task<InvoiceSequence?> GetBySeriesAsync(Guid companyId, string series, int fiscalYear, CancellationToken ct);
    Task<InvoiceSequence?> GetByIdAsync(Guid id, CancellationToken ct);
    Task AddAsync(InvoiceSequence entity, CancellationToken ct);
}
