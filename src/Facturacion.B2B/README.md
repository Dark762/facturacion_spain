# Facturacion.B2B

## Responsabilidad
Modelos y contratos de integración de facturación electrónica B2B. Depende de Domain, sin necesidad de arrancar un proceso.

## Configuración
Actualmente este proyecto de biblioteca no declara appsettings, User Secrets ni opciones de host propias. La configuración de un futuro proveedor de transmisión debe declararse en Configuration y en el host que lo utilice, nunca incrustada en B2B.

## Seguridad y evolución
Proteger identificadores fiscales y documentos; validar esquemas, estados e idempotencia antes de habilitar transmisiones externas. Distinguir contratos actuales de integraciones de producción pendientes.
