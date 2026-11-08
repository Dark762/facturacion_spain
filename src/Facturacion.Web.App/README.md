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
