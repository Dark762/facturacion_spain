# Organización de las pruebas

La solución separa los tests que no necesitan infraestructura de los que ejercitan componentes integrados.

## Proyectos

- `test/Facturacion.UnitTests`: pruebas unitarias puras, sin servidor HTTP ni SQLite. Contiene reglas de registro, verificación de firmas Stripe y vectores oficiales de huellas AEAT.
- `test/Facturacion.Integration.Test`: pruebas que ejercitan SQLite, el contexto multi-tenant, el pipeline HTTP, autenticación, emisión, XML/XSD, outbox y clientes HTTP. Conserva los fixtures compartidos en `Infrastructure/`.

## Política de limpieza

No eliminar pruebas solo porque el producto comercial aún no esté terminado. Los tests de comportamiento *fail closed* y los vectores fiscales son controles de seguridad que deben mantenerse. Antes de retirar cualquier prueba, comprobar que su contrato es realmente obsoleto y que no se pierde cobertura.

## Ejecución

```bash
dotnet build Facturacion.slnx
dotnet test test/Facturacion.UnitTests/Facturacion.UnitTests.csproj
dotnet test test/Facturacion.Integration.Test/Facturacion.Integration.Test.csproj
dotnet test Facturacion.slnx
```

La reorganización se hizo en `develop` sin ejecutar build/test. Las pruebas restantes no se han migrado en bloque para evitar romper dependencias entre fixtures. Como siguiente paso, una vez validada esta separación, se puede crear `Facturacion.ApiTests` si compensa aislar el pipeline HTTP de SQLite y los fixtures actuales.
