# Logging de FacturaciónES sin Aspire

Los cuatro hosts (API, Web.App, Web.Public y Workers) registran eventos mediante `ILogger<T>` y Serilog.

## Salida predeterminada
- Consola: JSON estructurado, apto para recolección por Docker, IIS/Azure u otro agente.
- Archivo: desactivado por defecto. Solo debe activarse en una ruta persistente con permisos de escritura y acceso restringido.
- Niveles: Debug en Development, Information en otros entornos; Microsoft en Warning y comandos EF en Error.

## Configuración opcional
En la configuración del host, o con variables de entorno (doble guion bajo), definir:

```json
{
  "FacturacionLogging": {
    "MinimumLevel": "Information",
    "EnableFile": true,
    "FilePath": "/var/log/facturaciones/facturacion-.json",
    "RetentionDays": 30
  }
}
```

En Windows, usar una ruta persistente de Windows en `FilePath`. En Docker, mantener `EnableFile=false` salvo volumen persistente. Cada host que escriba archivos debe usar una ruta propia; no compartir el mismo archivo entre procesos.

Los archivos rotan diariamente y al alcanzar 50 MiB, con límite de 30 archivos por defecto (configurable con `RetentionDays`; este nombre representa el máximo de archivos retenidos, **no** días exactos cuando hay múltiples archivos por día). Para retención temporal estricta usar políticas del colector/hosting.

## Seguridad
No registrar contraseñas, JWT, códigos de correo, tokens de checkout, cabeceras de autorización ni cuerpos de peticiones. Restringir el acceso a logs y aplicar retención conforme a privacidad. El identificador de traza de la respuesta API sirve para correlacionar el error con el registro del servidor.

## Pendiente
- Contexto autenticado TenantId/CompanyId con validación y minimización.
- Exportación centralizada a Seq/Loki/Azure según hosting.
- Alertas y trazas distribuidas OpenTelemetry.
- Validación de compilación y pruebas en el entorno de desarrollo.
