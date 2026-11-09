# Facturacion.Infrastructure

Implementaciones técnicas para el SaaS de **Facturación Spain** (.NET 8).

## Qué contiene

- `Persistence/BillingDbContext`: EF Core, mapeos de entidades y filtros por `TenantId`.
- `Persistence/Repositories`: repositorios de empresas, clientes, facturas, secuencias, registros fiscales, Outbox, auditoría y transmisiones.
- `Persistence/UnitOfWork`: persistencia y transacciones compartidas por los casos de uso.
- `Persistence/Providers`: selección de **MySQL, MariaDB, PostgreSQL y SQL Server** mediante configuración.
- `Identity`, `Multitenancy`: Identity, autorización por empresa y contexto de tenant.
- `Billing` y `Provisioning`: registro self-service, planes/suscripciones, eventos Stripe y provisión de tenants/empresas.
- `Email` e `Invitations`: SMTP y flujo de invitaciones.

## Multi-tenancy

`BillingDbContext` aplica filtros de consulta al tenant en las entidades de negocio y comprueba el `TenantId` de entidades `ITenantEntity` al guardar (incluida la prohibición de cambiarlo durante una modificación). Estas defensas complementan, pero **no sustituyen**, la validación de JWT, pertenencia y permisos de empresa en API/Application. Revisar también las operaciones de administración y toda consulta que evite filtros globales.

## Base de datos y configuración

La API usa `Database:Provider` y `Database:ConnectionStringName` (por defecto `Default`), resueltos a `ConnectionStrings:<nombre>`. Los esquemas SQL están en `../../database/{mysql,mariadb,postgresql,sqlserver}/`, con actualizaciones en `../../database/upgrade/`. **Revisar dependencias, versión y datos antes de ejecutar scripts**, especialmente en una base ya existente.

No guardar contraseñas, claves JWT ni secretos Stripe/SMTP en el repositorio. Utilizar variables de entorno o un almacén seguro.

```bash
dotnet build src/Facturacion.Infrastructure/Facturacion.Infrastructure.csproj
```

La presencia de repositorios Outbox y AEAT/B2B no significa que un Worker esté consumiendo los mensajes. Consultar [Application](../Facturacion.Application/README.md) y [README principal](../../README.md).
