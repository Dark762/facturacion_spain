# Facturacion.Web.App

## Responsabilidad
Interfaz Blazor Server privada. Consume la API por HTTP; no accede directamente a MySQL. Contiene `/login`, `/logout`, área de trabajo y `/platform`.

## Configuración
Archivo: `../Facturacion.Configuration/Hosts/Web.App/appsettings.json` y variante por entorno.
- `Facturacion:ApiBaseUrl`: URL absoluta HTTP(S) de la API. En Development se propone `https://localhost:7003`; comprobar el puerto real con Aspire.
- `FacturacionLogging:*` y `FacturacionMetrics:*`: opciones de observabilidad.
- No configurar aquí `Jwt:SigningKey`, `PlatformAdmin:UserId` ni `ConnectionStrings:Default` con credenciales reales.

## Autenticación
El formulario solicita email, contraseña y GUID de tenant porque `api/auth/login` exige pertenencia activa a ese tenant. El servidor recibe un JWT y crea una cookie de autenticación HttpOnly, Secure y SameSite=Lax; el JWT permanece en el ticket protegido por Data Protection. El acceso global se autoriza de nuevo en API mediante la política `PlatformAdmin`. Tener cookie no otorga permiso SuperAdmin.

## Desarrollo y despliegue
```powershell
dotnet user-secrets set "Facturacion:ApiBaseUrl" "https://localhost:7003" --project src/Facturacion.Web.App
```
En producción usar `Facturacion__ApiBaseUrl` y HTTPS. Persistir/compartir claves Data Protection si se necesitan sesiones entre reinicios o varias instancias. Validar flujo login, logout, caducidad de JWT y denegación de `/platform`.

## Limitación actual
El formulario requiere escribir manualmente el GUID del tenant. No hay selector de tenant. Consultar [centro de control](../../docs/observability/platform-admin.md).

## Nuevo flujo de inicio de sesión

`GET /login` renderiza `LoginView` (correo y contraseña). `POST /login` llama a `api/auth/login/start` y almacena el token de selección en una cookie temporal protegida, HttpOnly, Secure y SameSite Strict (máximo 5 minutos). `GET /select-tenant` renderiza `TenantSelectorView` de UI.Shared y muestra únicamente las organizaciones activas y, para el propietario, el Centro de Control. `POST /select-tenant` intercambia el token temporal por un JWT definitivo y crea la cookie de sesión. Los formularios POST validan antiforgery.

La selección no requiere que el usuario escriba un GUID. Para evitar exponer credenciales, los tokens temporales no se pasan en URLs. Si el usuario deja caducar la selección debe iniciar sesión de nuevo.

El cambio de tenant **durante** una sesión ya iniciada y los adaptadores de autenticación nativa MAUI quedan pendientes.
