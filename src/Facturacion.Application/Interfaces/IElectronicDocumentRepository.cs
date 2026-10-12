using Facturacion.Domain.Entities;
namespace Facturacion.Application.Interfaces;
public interface IElectronicDocumentRepository
{
    Task<ElectronicDocument?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<ElectronicDocument?> GetByInvoiceAsync(Guid invoiceId, CancellationToken ct);
    Task AddAsync(ElectronicDocument entity, CancellationToken ct);
}
