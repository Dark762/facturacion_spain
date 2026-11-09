# Facturacion.Integration.Test

Proyecto de pruebas xUnit sobre **.NET 8**, con `Microsoft.AspNetCore.Mvc.Testing` y soporte SQLite para escenarios de integración.

## Cobertura presente en el repositorio

Las clases de prueba cubren, entre otras áreas: pipeline de API, autenticación y autorización por empresa, aislamiento multi-tenant, borradores y emisión, resolución de proveedor de base de datos, alta SaaS, invitaciones, firmas Stripe, estado fail-closed del motor fiscal, vectores de huella y herramientas XSD.

**Una prueba sobre el motor fiscal no significa que pueda emitirse legalmente**: hay pruebas específicas de bloqueo cuando los componentes requeridos no están listos. Los XSD bajo `xsd/` están configurados para copiarse al output del proyecto de pruebas, pero la implementación XSD de producción continúa no disponible.

## Ejecutar

Desde la raíz:

```bash
dotnet test test/Facturacion.Integration.Test/Facturacion.Integration.Test.csproj
```

Revisar `Infrastructure/` y sus fixtures antes de ejecutar pruebas de persistencia; usar siempre bases y credenciales de **prueba**, nunca producción. Esta actualización documental no incluye ejecución ni verificación de resultados.

Ver [README principal](../../README.md).
