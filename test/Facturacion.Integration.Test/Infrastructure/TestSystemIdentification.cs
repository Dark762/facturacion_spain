using Facturacion.Application.Services;

namespace Facturacion.Integration.Test.Infrastructure;

/// <summary>
/// Identificacion del sistema informatico para pruebas.
///
/// NO es una identificacion real: son valores ficticios que cumplen los
/// limites del esquema oficial para poder ejercitar el camino de emision.
/// En produccion estos valores vienen del entorno, y si faltan la emision
/// queda bloqueada.
/// </summary>
public static class TestSystemIdentification
{
    public static FiscalSystemIdentification Valid() => new()
    {
        ProducerName = "EMPRESA PRODUCTORA DE PRUEBAS",
        ProducerNif = "B12345678",
        SystemName = "FacturacionSaaS",
        SystemId = "TS",
        SystemVersion = "0.1.0",
        InstallationNumber = "INSTALACION-PRUEBA-0001"
    };
}