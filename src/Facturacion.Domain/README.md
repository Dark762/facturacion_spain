# Facturacion.Domain

## Responsabilidad
Modelo de dominio independiente de HTTP, EF Core y UI. Define entidades como Tenant, Company, Customer, Invoice y sus reglas, enumeraciones e interfaces de dominio.

## Configuración
No carga appsettings, User Secrets ni variables de entorno. No añadir aquí claves JWT, Stripe, SMTP o cadenas de conexión. Las reglas del dominio deben funcionar sin servicios externos.

## Multi-tenant
Las entidades de cliente utilizan TenantId cuando corresponde. El aislamiento real se refuerza en Infrastructure (filtros y validación) y API (autenticación y pertenencia); no confiar solo en el modelo.

## Al modificar
Revisar contratos, migraciones de persistencia, validaciones de Application y pruebas de aislamiento. No incorporar dependencias de Infrastructure o Web.
