using Facturacion.Application.Common.Results;
using Facturacion.Application.Interfaces;
using Facturacion.Domain.Entities;

namespace Facturacion.Application.Services;

/// <summary>
/// Fail-closed: la creación de un alta VERI*FACTU requiere un motor fiscal validado
/// (XML, huella, encadenamiento, numeración y bloqueo transaccional).
/// No crea registros de apariencia válida ni cambia el estado de una factura.
/// </summary>
public sealed class FiscalRecordService : IFiscalRecordService
{
    public Task<Result<Guid>> CreateAltaAsync(Invoice invoice, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(invoice);
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(Result<Guid>.Failure(new Error(
            "FISCAL_ENGINE_NOT_CONFIGURED",
            "La emisión VERI*FACTU no está habilitada: falta el motor fiscal y el encadenamiento transaccional.",
            ErrorType.BusinessRule)));
    }
}
