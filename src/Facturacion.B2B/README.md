# Facturacion.B2B

Proyecto .NET 8 reservado para la **integración futura de factura electrónica B2B**.

## Estado actual

Este ensamblado solo contiene `Class1.cs`, una plantilla vacía. **No incluye generación Facturae/UBL, adaptadores de operadores, transmisión ni confirmación externa**.

Existen preparativos B2B en otros proyectos:

- `Facturacion.Application`: contratos y `B2BInvoiceService`, que valida la factura y un documento ya preparado y crea un mensaje Outbox.
- `Facturacion.Domain`: entidades `ElectronicDocument` y `B2BTransmission`.
- `Facturacion.Infrastructure`: repositorios para dichos modelos.

El Outbox **no es una confirmación de entrega**: el Worker actual todavía no ejecuta la transmisión.

## Evolución prevista

Ubicar aquí implementaciones concretas de formatos, transporte o proveedores, con tests de esquema, idempotencia, errores, seguridad y recibos. Mantenerlas desacopladas de la factura comercial y de VERI*FACTU; son obligaciones/integraciones distintas.

```bash
dotnet build src/Facturacion.B2B/Facturacion.B2B.csproj
```

Ver [README principal](../../README.md).
