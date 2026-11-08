# Facturacion.Verifactu

## Responsabilidad
Contratos y servicios de validación/formato fiscal VERI*FACTU; depende de Domain. Se integra en API y Workers.

## Configuración
`Fiscal:SchemaDirectory` indica la ubicación de XSD fiscales; está declarada en `Shared/appsettings.json` y puede sobrescribirse por host/entorno. La inicialización usa `AddVerifactu(fiscalSchema.SchemaDirectory)`.

## Operación
Comprobar que los XSD existen y se distribuyen al publicar. La validación local de XML no equivale a una aceptación por la AEAT. Revisar normativa, versionado de esquemas y pruebas fiscales antes de emitir o remitir documentos.

## Seguridad
Evitar registrar XML fiscal completo o datos personales en trazas. Mantener las credenciales de integraciones externas en los hosts, si se incorporan.
