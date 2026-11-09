# Facturacion.Application

Capa de **casos de uso** de Facturación Spain (.NET 8). Coordina el dominio, repositorios e integraciones mediante contratos, sin ser la API HTTP ni la capa de persistencia.

## Contenido real

- Resultados y errores: `Result`, `Result<T>`, `Error`, `ErrorType` y `ApiResponse<T>`.
- Interfaces para repositorios, contexto de tenant, permisos por empresa, unidad de trabajo, AEAT, B2B y servicios de facturas.
- DTO de facturas, empresas, clientes, fiscalidad y transmisiones.
- `InvoiceApplicationService`: crear y consultar **borradores** con autorización por empresa, EUR, IVA 0/4/10/21 y redondeo por línea. IVA 0 **no** implica por sí mismo exención o no sujeción.
- `InvoiceIssuanceService`: flujo transaccional que valida preparación fiscal, bloquea la secuencia, asigna número, genera huella y cadena, crea registro y Outbox, y confirma todo o revierte. Su disponibilidad **depende del motor fiscal**.
- `AeatSubmissionService` y `B2BInvoiceService`: preparan operaciones y **encolan**, sin remitir por sí mismos a plataformas externas.
- `AddBillingApplication()`: registra servicios de aplicación en el contenedor DI.

## Estado de la emisión

**En el estado actual, la emisión está deshabilitada**: `IFiscalEngine.IsReadyForIssuance` es falso porque faltan generador XML y validador XSD operativos. El caso de uso devuelve un error antes de consumir la numeración. No confundir implementación del flujo transaccional con capacidad legal de emitir.

El servicio usa `IUnitOfWork`, `IInvoiceSequenceRepository`, `IFiscalRecordRepository`, `IFiscalChainRepository` y `IOutboxMessageRepository`. La numeración y el registro fiscal deben mantenerse atómicos. La integración AEAT/B2B y el consumo del Outbox son trabajo pendiente.

## Dependencias y uso

La capa depende de `Facturacion.Domain` y de contratos del motor `Facturacion.Verifactu`. Las implementaciones de repositorios, seguridad y contexto de tenant se registran desde Infrastructure y Api.

```bash
dotnet build src/Facturacion.Application/Facturacion.Application.csproj
```

No mover lógica HTTP ni conexiones directas a la base de datos a esta capa. Respetar el aislamiento por tenant y la autorización por empresa. Véanse [README principal](../../README.md) y [motor fiscal](../Facturacion.Verifactu/README.md).
