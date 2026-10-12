using Facturacion.Domain.Entities;
namespace Facturacion.Application.Interfaces;
public interface IFiscalRecordRepository
{
    Task<IReadOnlyList<FiscalRecord>> GetByInvoiceAsync(Guid invoiceId, CancellationToken ct);
    Task<FiscalRecord?> GetByIdAsync(Guid id, CancellationToken ct);
    Task AddAsync(FiscalRecord entity, CancellationToken ct);
}
