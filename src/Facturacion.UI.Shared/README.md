# Facturacion.UI.Shared

## Responsabilidad
Componentes y estilos de interfaz reutilizables entre Web.App y Web.Public, incluidos elementos del espacio de trabajo.

## Configuración
No hospeda procesos, no carga appsettings y no debe contener credenciales. Las URLs, autenticación y servicios se configuran en cada host consumidor. La hoja de estilos compartida se publica como recurso estático `_content/Facturacion.UI.Shared/design.css`.

## Seguridad
No poner datos sensibles ni comprobaciones de permisos solo en componentes visuales. Toda autorización real debe comprobarse en la API. Evitar dependencias directas de DbContext o Stripe.

## Cambios
Comprobar que las modificaciones visuales funcionan tanto en Web.App como en Web.Public y que los componentes no presuponen un tenant fijo.

## Login reutilizable (Web, Desktop y Mobile)

`Components/LoginView.razor` es un componente **de presentación** con parámetros `AntiforgeryToken` y `ShowError`. Su CSS está en `wwwroot/login.css` y se publica como `_content/Facturacion.UI.Shared/login.css`.

`Facturacion.Web.App` lo renderiza en GET `/login` con `HtmlRenderer`; el POST `/login` permanece en el servidor para validar antiforgery, llamar a `api/auth/login` y emitir la cookie segura. Así se evita que el componente compartido maneje contraseñas o secretos de autenticación.

**Desktop/Mobile:** un futuro host Blazor Hybrid (.NET MAUI) puede reutilizar la presentación y el CSS. El formulario HTML actual utiliza POST a `/login`, apropiado para el host web; para Blazor Hybrid será necesario conectar el componente a un flujo de autenticación nativo (cliente API + almacenamiento seguro del token), en lugar de depender de cookies del servidor. No se ha implementado todavía ese host ni su adaptador.

Al modificar la vista, mantener campos `email`, `password`, `tenantId` y el token antiforgery para conservar compatibilidad con el endpoint web.

## Selector de tenant

`Components/TenantSelectorView.razor` presenta las organizaciones disponibles por nombre y una entrada de plataforma opcional. No autentica ni almacena tokens. Web.App le proporciona `Tenants`, `CanAccessPlatform` y el token antiforgery. Desktop y Mobile pueden reutilizar su presentación y conectar el evento de selección a su propio adaptador de autenticación cuando existan esos hosts.
