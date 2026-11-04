# Centro de control: primera fase (API segura)

## Seguridad
Los endpoints `GET /api/platform/summary` y `GET /api/platform/tenants?page=1&pageSize=25` requieren un JWT válido y la política `PlatformAdmin`. La política **deniega por defecto**: solo acepta el identificador Identity exacto configurado en el servidor con `PlatformAdmin__UserId` (o `PlatformAdmin:UserId`). No usar email, nombres, roles `Admin` de empresa ni una lista editable por clientes. Nunca incluir el ID de propietario en código fuente ni en configuración pública.

El JWT actual requiere pertenencia activa a un tenant: el administrador de plataforma debe iniciar sesión mediante el mecanismo de autenticación existente con un tenant del que sea miembro. El middleware de tenant continúa verificando esa pertenencia antes de permitir acceso. La autorización global adicional se aplica en el controlador y no otorga acceso global a los endpoints de clientes.

## Consultas
`summary` devuelve número de tenants, tenants activos y empresas activas. `tenants` devuelve páginas de hasta 100 tenants, número de empresas activas, plan, estado de suscripción y fecha de registro cuando existe. Las consultas globales a empresas usan `IgnoreQueryFilters()` únicamente en este controlador con política `PlatformAdmin`. No se devuelven NIF, emails, contraseñas, direcciones ni tokens.

## Web.App: autenticación y panel

`GET /login` sirve un formulario HTML desde el servidor; `POST /login` valida antiforgery y comprueba credenciales contra `POST /api/auth/login`. El usuario debe indicar email, contraseña y GUID de un tenant del que sea miembro activo, porque la API actual exige ese identificador para emitir JWT. No se crea un nuevo endpoint de autenticación sin tenant.

La Web.App crea una cookie de autenticación HttpOnly, Secure, SameSite=Lax, sin persistencia y con expiración ajustada a la del JWT de API. El JWT se almacena dentro del ticket de cookie protegido por ASP.NET Core Data Protection, no en localStorage ni en JavaScript. `/platform` consulta `api/platform/summary` y `api/platform/tenants` en el servidor usando el JWT; la API valida además `PlatformAdmin__UserId`. Las demás rutas de Razor Components también requieren cookie autenticada.

**Requisitos para producción**: establecer `Facturacion:ApiBaseUrl` en Web.App; configurar `PlatformAdmin__UserId` solo en API; publicar ambos servicios sobre HTTPS; persistir y compartir el anillo de claves de Data Protection si hay varios procesos/instancias o reinicios que deban conservar sesiones. La cookie no sustituye la validación de seguridad_stamp en la API. No exponer `api_token` en HTML, trazas o logs.

`POST /logout` elimina la cookie y requiere antiforgery. La interfaz de inicio de sesión utiliza el GUID del tenant por ahora; un selector amigable requerirá un flujo de descubrimiento de tenants autenticado y seguro.

## Pruebas mínimas
- Sin `PlatformAdmin__UserId`: 403 con token de tenant válido.
- Sin token: 401.
- Con token de usuario tenant-admin diferente del propietario: 403.
- Con token del propietario y pertenencia activa al tenant: 200.
- Con JWT de propietario revocado o tenant sin pertenencia activa: 401/403.
- Comprobar que `GET /api/platform/tenants` pagina y no muestra información fiscal sensible.

No se han añadido migraciones de base de datos en esta fase.
