namespace Facturacion.Application.Interfaces;

using Facturacion.Application.Services;

/// <summary>
/// Proveedor de identificacion del sistema fiscal.
/// Permite inyeccion de dependencias tipada, valores inmutables por operacion
/// y aislamiento entre pruebas sin depender de estado estatico global.
/// </summary>
public interface IFiscalSystemProvider
{
    FiscalSystemIdentification Current { get; }
    bool IsComplete { get; }
    string Describe();
}
