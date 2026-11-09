# Facturacion.Verifactu

Motor de cumplimiento normativo **separado del producto SaaS de facturación comercial**. Su objetivo es encapsular reglas y formatos fiscales sin imponerlos como fundamento de la lógica comercial.

> **Estado actual: emisión fiscal bloqueada por diseño (fail-closed).** El código no debe presentarse como certificación o integración VERI*FACTU lista para producción.

## Componentes observados

| Componente | Estado en el código |
| --- | --- |
| Validación de datos internos (`Verifier`) | Disponible; reglas internas, no certificación normativa |
| Builders de alta y anulación | Modelos disponibles; no serializan XML |
| Canonicalización | Implementada y contrastada con vectores oficiales incluidos en pruebas |
| Huella SHA-256 | Implementada y contrastada con vectores oficiales |
| Encadenamiento | Implementado |
| XML fiscal | `UnavailableRecordXmlBuilder` |
| Validación XSD en ejecución | `UnavailableXsdValidator` |
| Generación de QR | `UnavailableQrGenerator` |

`FiscalSpecification` centraliza estados, componentes obligatorios y bloqueos. `IFiscalEngine.IsReadyForIssuance` es falso mientras XML/XSD no estén disponibles, y `InvoiceIssuanceService` rechaza la operación **antes de consumir un número de factura**.

**Nota importante:** existen archivos XSD en `../../docs/verifactu/xsd/` y en el proyecto de pruebas, pero el adaptador XSD de producción sigue sin implementarse. La existencia de esos archivos no habilita automáticamente la validación ni la remisión. El QR figura asimismo no disponible, aunque no está incluido en la lista de componentes que bloquean la emisión: esto **no** equivale a aprobar el flujo de factura completo.

## Integración y desarrollo

Registro DI mediante `AddVerifactu()`. Mantener las verificaciones oficiales y sus versiones reproducibles, con pruebas que confirmen canonicalización, huellas, estructura XML, XSD, QR y casos de error. No generar XML aproximado ni simular una aceptación de AEAT.

```bash
dotnet test test/Facturacion.Integration.Test/Facturacion.Integration.Test.csproj
```

La fuente normativa y la versión de huellas consideradas están indicadas en `FiscalSpecification.cs`; verificar actualizaciones oficiales antes de implantar cambios. Consulta la [documentación general](../../README.md).
