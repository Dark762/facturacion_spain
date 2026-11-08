# Facturacion.Workers

## Responsabilidad
Procesamiento asíncrono de Outbox y operaciones fiscales, separado de las peticiones HTTP.

## Configuración
Archivos: `../Facturacion.Configuration/Hosts/Workers/appsettings.json` y variantes por entorno, más Shared.
- `ConnectionStrings:Default`: obligatoria, en User Secrets o variables de entorno.
- `Database:Provider` y `Database:ConnectionStringName`.
- `Outbox:BatchSize`, `Outbox:PollInterval`, `Outbox:Lease`, `Outbox:MaxAttempts`, `Outbox:BaseDelay`.
- `Fiscal:SchemaDirectory`: ruta de esquemas XSD.
- `FacturacionLogging:*` y `FacturacionMetrics:*` para diagnóstico.

## Desarrollo
```powershell
dotnet user-secrets set "ConnectionStrings:Default" "CADENA_MYSQL_LOCAL" --project src/Facturacion.Workers
```
El worker utiliza la misma base de datos que API/Web.Public. No necesita JWT de API ni clave Stripe para su configuración actual.

## Precauciones
El worker procesa datos de varios tenants; cada operación debe fijar el contexto correcto. Vigilar reintentos, bloqueos de Outbox, idempotencia y errores. No ejecutar dos consumidores incompatibles contra la misma cola sin revisar leases.
