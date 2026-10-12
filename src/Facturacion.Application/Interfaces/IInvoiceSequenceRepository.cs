using Facturacion.Domain.Entities;
namespace Facturacion.Application.Interfaces;
public interface IInvoiceSequenceRepository
{
    Task<InvoiceSequence?> GetBySeriesAsync(Guid companyId, string series, int fiscalYear, CancellationToken ct);
    Task<InvoiceSequence?> GetByIdAsync(Guid id, CancellationToken ct);
    Task AddAsync(InvoiceSequence entity, CancellationToken ct);

    /// <summary>
    /// Lee la secuencia y la bloquea para escritura hasta el fin de la transaccion
    /// actual. Es la garantia de que dos emisiones concurrentes no obtienen el
    /// mismo numero de factura.
    /// </summary>
    Task<InvoiceSequence?> LockBySeriesAsync(Guid companyId, string series, int fiscalYear, CancellationToken ct);
}
