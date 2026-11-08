# Facturacion.UI.Shared

## Responsabilidad
Componentes y estilos de interfaz reutilizables entre Web.App y Web.Public, incluidos elementos del espacio de trabajo.

## Configuración
No hospeda procesos, no carga appsettings y no debe contener credenciales. Las URLs, autenticación y servicios se configuran en cada host consumidor. La hoja de estilos compartida se publica como recurso estático `_content/Facturacion.UI.Shared/design.css`.

## Seguridad
No poner datos sensibles ni comprobaciones de permisos solo en componentes visuales. Toda autorización real debe comprobarse en la API. Evitar dependencias directas de DbContext o Stripe.

## Cambios
Comprobar que las modificaciones visuales funcionan tanto en Web.App como en Web.Public y que los componentes no presuponen un tenant fijo.
