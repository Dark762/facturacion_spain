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

## Mi cuenta, cambio de contraseña y cierre de sesión

- `GET /account`: obtiene el perfil autenticado de la API y renderiza el componente `AccountView` de `Facturacion.UI.Shared`.
- `POST /account/change-password`: formulario protegido con antiforgery; envía contraseña actual y nueva a `api/auth/me/change-password`. Si tiene éxito, cierra la cookie local y devuelve al login.
- `POST /logout`: cierre de sesión mediante POST con antiforgery; el menú ofrece enlaces visibles a **Mi perfil y seguridad** y **Cerrar sesión** (sección de cierre en la página de cuenta).
- No se muestran ni se guardan contraseñas en la sesión; el JWT continúa protegido dentro de la cookie HttpOnly del host web.
- El formulario y CSS son responsive y están centralizados en UI.Shared; los futuros clientes MAUI necesitarán adaptar la acción a su propio mecanismo de autenticación.

### Cuenta interactiva sin JavaScript propio

La ruta `/account` ahora es `Components/Pages/Account.razor` con `@rendermode InteractiveServer`. Obtiene el JWT desde el estado de autenticación del servidor y consulta el perfil mediante `FacturacionAuthApi`. El formulario de contraseña sigue enviándose al endpoint BFF `POST /account/change-password` con `AntiforgeryToken`.

`Facturacion.UI.Shared/Components/LogoutConfirmation.razor` muestra la confirmación mediante estado y eventos Blazor (`@onclick`), integrado en `AccountView.razor`. El enlace del menú `/account?confirmLogout=1` abre la confirmación. El cierre definitivo sigue siendo `POST /logout`, protegido con antiforgery; cancelar no ejecuta ninguna petición. Se eliminó `account.js` y el renderizado manual con `HtmlRenderer` de `/account`.

No se utiliza JavaScript propio. La referencia a `_framework/blazor.web.js` en `App.razor` es **el runtime oficial de Blazor**, necesario para los eventos interactivos en Web; los hosts MAUI/desktop utilizan su runtime Blazor Hybrid correspondiente. El componente compartido no depende de APIs del navegador.

## Modal de cierre de sesión sin JavaScript propio

La página `Components/Pages/Account.razor` utiliza `@rendermode InteractiveServer` y presenta `Facturacion.UI.Shared.Components.AccountView`. Este componente administra la visibilidad de `LogoutConfirmation.razor` mediante estado Blazor, `@onclick` y `EventCallback`. No existe `account.js` ni se necesita interoperabilidad JavaScript para abrir o cancelar el diálogo.

La entrada del menú `/account?confirmLogout=1` abre la confirmación al entrar a la página; el botón «Cerrar sesión» también la abre. La acción confirmada usa un formulario POST normal a `/logout` con `<AntiforgeryToken />`; la cookie HttpOnly solo se elimina desde el servidor. La página de cuenta usa `GET /api/auth/me/profile` con el JWT obtenido del estado autenticado.

La interfaz es compartida con Blazor Hybrid; el formulario POST a `/logout` es específico del host web y deberá abstraerse mediante un servicio de sesión para MAUI cuando se incorpore ese host. La capa de componentes no debe almacenar tokens.
