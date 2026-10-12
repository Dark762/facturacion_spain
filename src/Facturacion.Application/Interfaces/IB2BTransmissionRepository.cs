using Facturacion.Domain.Entities;
namespace Facturacion.Application.Interfaces;
public interface IB2BTransmissionRepository
{
    Task<B2BTransmission?> GetByIdAsync(Guid id, CancellationToken ct);
    Task AddAsync(B2BTransmission entity, CancellationToken ct);
}
