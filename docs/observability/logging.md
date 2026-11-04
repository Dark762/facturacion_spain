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
- Contexto multiempresa: incorporar identificadores únicamente tras autorización; no copiar claims o cabeceras sin validar.
- Exportación centralizada a Seq/Loki/Azure según hosting.
- Alertas y trazas distribuidas OpenTelemetry.
- Validación de compilación y pruebas en el entorno de desarrollo.

## Correlación HTTP
El enriquecedor `HttpRequestLogEnricher` incorpora `TraceId` y `RequestPath` cuando existe una petición HTTP. En la API, `TenantResolutionMiddleware` agrega `TenantId` y `UserId` a `HttpContext.Items` únicamente después de comprobar pertenencia activa del usuario al tenant.

`CompanyId` se incorpora únicamente después de una autorización satisfactoria mediante `CompanyAuthorizationService`; operaciones que no requieren esa autorización no tendrán esa propiedad. Los hosts sin contexto HTTP (Workers) siguen registrando eventos sin estas propiedades. No se confía en cabeceras ni claims de tenant sin validación.

Los errores capturados por `ApiExceptionHandler` conservan el mismo `TraceId` y pueden buscarse en el JSON de salida. La correlación distribuida entre procesos y los identificadores de empresa se implementarán por separado.

## Eventos críticos
`StripeEventProcessor` registra recepción, deduplicación, procesamiento correcto y activación de suscripciones. Evita registrar el contenido del webhook y credenciales. Los fallos de aprovisionamiento continúan registrándose con excepción y RegistrationId.

**Límite de alcance:** el contexto de empresa solo existe cuando una operación invoca explícitamente la autorización de empresa; no se infiere de parámetros de ruta ni de cuerpos HTTP. La instrumentación detallada del flujo de registro y aprovisionamiento se ampliará por etapas.

## Duración y resultado de operaciones
`Facturacion.Observability.OperationTelemetry` mide el tiempo transcurrido con `Stopwatch` y registra `Operation`, `RegistrationId`, `DurationMs` y `Outcome` (`Success`, `Failure`, `Cancelled`). Se aplica al checkout SaaS y al aprovisionamiento de tenants. No modifica el resultado ni captura las excepciones: las vuelve a lanzar para preservar los mecanismos de error y reintento. No registra contraseñas, tokens, NIF ni payloads de Stripe. Estos eventos son logs estructurados; todavía no son métricas agregadas ni alertas automáticas. Para investigar lentitud, filtrar `Operation` y ordenar `DurationMs`.
