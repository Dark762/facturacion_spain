# Facturacion.Observability

## Responsabilidad
Logging estructurado con Serilog, contexto HTTP validado y métricas OpenTelemetry para operaciones críticas. Es una biblioteca, no un recurso independiente de Aspire.

## Configuración por host
- `FacturacionLogging:MinimumLevel`: Debug/Information/Warning/Error; predeterminado Debug en Development e Information en otros entornos.
- `FacturacionLogging:EnableFile`: habilita archivos rotativos (por defecto false).
- `FacturacionLogging:FilePath`: obligatorio si EnableFile=true.
- `FacturacionLogging:RetentionDays`: número de archivos retenidos, no necesariamente días exactos.
- `FacturacionLogging:Centralized:Enabled`: activa el sink Seq opcional (false por defecto).
- `FacturacionLogging:Centralized:Provider`: `Seq` cuando está habilitado.
- `FacturacionLogging:Centralized:ServerUrl`: URL HTTP(S) absoluta sin credenciales.
- `FacturacionLogging:Centralized:ApiKey`: clave opcional; suministrar como secreto o variable de entorno, nunca en Git.
- `FacturacionMetrics:EnableOtlp`: habilita exportación OTLP.
- `FacturacionMetrics:OtlpEndpoint`: URL HTTP(S) absoluta opcional; Aspire puede suministrar `OTEL_EXPORTER_OTLP_ENDPOINT`.

## Métricas y eventos
Meter `Facturacion.Operations`: `facturacion.operation.completed` y `facturacion.operation.duration`, etiquetadas por operación y resultado. `OperationTelemetry` mide duración de checkout y aprovisionamiento. Los logs enriquecidos pueden incluir TraceId, TenantId, UserId y CompanyId solo después de validación.

## Seguridad
No incluir contraseñas, JWT, datos bancarios ni información fiscal personal en logs. En producción, habilitar almacenamiento/retención centralizados o archivos persistentes: el dashboard local de Aspire no almacena histórico de forma permanente.

Ver [guía de logging](../../docs/observability/logging.md).
