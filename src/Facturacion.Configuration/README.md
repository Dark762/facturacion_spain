# Facturacion.Configuration

## Responsabilidad
Única ubicación de JSON de configuración por host y entorno, contratos tipados de opciones y validadores.

## Estructura
- `Shared/appsettings.json`: Database, ConnectionStrings (vacía por defecto), Identity, Invitations, Fiscal y niveles de log.
- `Shared/appsettings.{Environment}.json`: ajustes comunes por entorno.
- `Hosts/Api/appsettings*.json`: JWT, PlatformAdmin, Stripe, Email, Cors y URLs.
- `Hosts/Web.App/appsettings*.json`: `Facturacion:ApiBaseUrl`.
- `Hosts/Web.Public/appsettings*.json`: URLs del registro, Stripe y SMTP.
- `Hosts/Workers/appsettings*.json`: Outbox y configuración de worker.

## Precedencia
La extensión `AddFacturacionConfiguration` carga Shared, Shared por entorno, Host y Host por entorno. Los hosts añaden User Secrets en Development; la API también los carga en Local. Las variables de entorno se escriben con `__`, por ejemplo `Jwt__SigningKey`.

## Secretos
No poner valores reales en JSON versionado. Consultar [guía local](../../docs/configuration/local-secrets.md) para comandos por proyecto.

## Al agregar una clave
Elegir host o Shared según alcance, añadirla al contrato Options si aplica, validar cuando sea obligatoria, documentarla y revisar su comportamiento en Development, Local, Staging y Production.
