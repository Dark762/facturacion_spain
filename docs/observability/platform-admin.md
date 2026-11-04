# Centro de control: primera fase (API segura)

## Seguridad
Los endpoints `GET /api/platform/summary` y `GET /api/platform/tenants?page=1&pageSize=25` requieren un JWT válido y la política `PlatformAdmin`. La política **deniega por defecto**: solo acepta el identificador Identity exacto configurado en el servidor con `PlatformAdmin__UserId` (o `PlatformAdmin:UserId`). No usar email, nombres, roles `Admin` de empresa ni una lista editable por clientes. Nunca incluir el ID de propietario en código fuente ni en configuración pública.

El JWT actual requiere pertenencia activa a un tenant: el administrador de plataforma debe iniciar sesión mediante el mecanismo de autenticación existente con un tenant del que sea miembro. El middleware de tenant continúa verificando esa pertenencia antes de permitir acceso. La autorización global adicional se aplica en el controlador y no otorga acceso global a los endpoints de clientes.

## Consultas
`summary` devuelve número de tenants, tenants activos y empresas activas. `tenants` devuelve páginas de hasta 100 tenants, número de empresas activas, plan, estado de suscripción y fecha de registro cuando existe. Las consultas globales a empresas usan `IgnoreQueryFilters()` únicamente en este controlador con política `PlatformAdmin`. No se devuelven NIF, emails, contraseñas, direcciones ni tokens.

## Interfaz privada: requisito pendiente
`Facturacion.Web.App` todavía no implementa un inicio de sesión seguro ni una sesión autenticada para acceder a la API. **No crear ni publicar una ruta /platform sin autenticación de servidor.** Antes de incorporar la pantalla Blazor es necesario completar autenticación de la Web App y el envío seguro del JWT a la API. Hasta entonces, los endpoints se pueden verificar con un cliente autenticado y un JWT de propietario; el dashboard visual queda pendiente.

## Pruebas mínimas
- Sin `PlatformAdmin__UserId`: 403 con token de tenant válido.
- Sin token: 401.
- Con token de usuario tenant-admin diferente del propietario: 403.
- Con token del propietario y pertenencia activa al tenant: 200.
- Con JWT de propietario revocado o tenant sin pertenencia activa: 401/403.
- Comprobar que `GET /api/platform/tenants` pagina y no muestra información fiscal sensible.

No se han añadido migraciones de base de datos en esta fase.
