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

## OpenTelemetry Metrics
El proyecto `Facturacion.Observability` incorpora un Meter llamado `Facturacion.Operations`. `OperationTelemetry` emite un contador `facturacion.operation.completed` y un histograma de duración `facturacion.operation.duration` (segundos), ambos etiquetados por `operation` y `outcome` (`Success`, `Failure`, `Cancelled`). No se usan RegistrationId, TenantId, CompanyId ni datos personales como etiquetas, para limitar la cardinalidad.

Por defecto las métricas se recogen mediante OpenTelemetry en el proceso, pero no se exportan fuera. Para habilitar el envío a un collector OTLP compatible, configurar:

```json
{
  "FacturacionMetrics": {
    "EnableOtlp": true,
    "OtlpEndpoint": "http://localhost:4317"
  }
}
```

El exportador usa OTLP/gRPC por defecto; el endpoint debe ser HTTP(S) absoluto y estar accesible desde cada host. La configuración de red, TLS y autenticación del collector depende del despliegue. En producción, utilizar TLS y no incluir secretos en los logs. No se incluye collector ni panel de métricas; hasta configurar un backend OTLP no habrá visualización histórica. Los eventos de logging JSON siguen funcionando de forma independiente.

## Panel local con Aspire
Inicia `Facturacion.AppHost` con el perfil `Development` en Visual Studio. Aspire aporta su propio receptor OTLP y dashboard. El AppHost habilita `FacturacionMetrics__EnableOtlp=true` para API, Web Pública, Web App y Workers en `Development` y `Local`; en `Staging` y `Production` lo deja en `false`. El exportador OpenTelemetry utiliza `OTEL_EXPORTER_OTLP_ENDPOINT` inyectado por Aspire cuando no se establece `FacturacionMetrics:OtlpEndpoint` explícitamente.

Realiza un alta de prueba o un checkout y abre la pestaña **Metrics** del dashboard Aspire. Busca el meter `Facturacion.Operations` y las métricas `facturacion.operation.completed` y `facturacion.operation.duration`. Las métricas de negocio solo aparecen tras ejecutar esas operaciones. El dashboard local es para diagnóstico de desarrollo, no almacenamiento histórico ni alertas persistentes. En despliegues independientes, configura `FacturacionMetrics:EnableOtlp=true` y un receptor OTLP externo, con TLS y credenciales según el entorno.

## Errores de entrega de correo en el registro SaaS

`SmtpEmailSender` clasifica fallos como `EMAIL_NOT_CONFIGURED`, `EMAIL_SMTP_TEMPORARY`, `EMAIL_SMTP_REJECTED` y `EMAIL_TRANSPORT_UNAVAILABLE`, sin incluir destinatarios ni códigos en los eventos propios. `RegisterAsync` y `ResumeAsync` instrumentan el envío mediante `OperationTelemetry` con las operaciones `RegistrationEmailRegister` y `RegistrationEmailResume`, y registran `RegistrationId`, `ErrorCode` e `IsTransient`.

La API transforma `EmailDeliveryException` en HTTP 503 con un mensaje seguro y `TraceId`. Para evitar enumeración de cuentas, `ResumeAsync` conserva su respuesta genérica ante fallos de entrega clasificados; el error queda registrado para operaciones. Los logs de excepciones de infraestructura pueden contener información del proveedor SMTP: restringir el acceso y revisar la política de redacción del colector.

Esta entrega **no** incorpora reintentos automáticos, persistencia de incidencias ni panel administrativo. Tampoco garantiza entrega final al buzón: una respuesta SMTP satisfactoria solo confirma la aceptación por el servidor SMTP.
