# Facturacion.Infrastructure

## Responsabilidad
Persistencia EF Core, ASP.NET Identity, autorización por empresa, multi-tenancy, aprovisionamiento, Stripe/registro SaaS y correo. Implementa interfaces de Application.

## Configuración que recibe de los hosts
- `Database:Provider` y `Database:ConnectionStringName` desde Shared o host.
- `ConnectionStrings:Default` desde User Secrets o variables de entorno (nunca en Git).
- `Identity:Policy:*` para contraseña y bloqueo.
- `Stripe:SecretKey` y `Stripe:WebhookSecret` si se activan pagos.
- `Email:*` y `Email:Smtp:*` para envíos.

## Contextos
`BillingDbContext` contiene entidades de negocio y filtros por TenantId; `AuthDbContext` gestiona Identity, TenantUsers y CompanyUsers; `SelfServiceDbContext` gestiona altas y suscripciones y no aplica filtro tenant, por lo que debe usarse exclusivamente desde servicios autorizados.

## Seguridad
No utilizar `IgnoreQueryFilters()` en servicios de clientes sin una justificación y autorización explícita. Nunca registrar contraseñas, tokens, NIF o datos fiscales completos. Mantener el orden de aprovisionamiento y las restricciones FK.

## Operación
Los scripts de actualización están en `database/upgrade`. Revisar migraciones, proveedor MySQL/MariaDB y pruebas de aislamiento antes de desplegar.
