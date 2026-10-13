# Configuración transversal (develop)

`src/Facturacion.Configuration/appsettings.Shared.json` contiene exclusivamente valores comunes **no sensibles**. Se copia como `appsettings.Shared.json` al directorio de contenido/salida de API, Workers y ambas webs. `Facturacion.Configuration` añade esta fuente **con la prioridad más baja**; por tanto prevalecen `appsettings.json`, `appsettings.{Environment}.json`, User Secrets, variables de entorno y argumentos del host.

## Qué se mantiene por aplicación

- API: JWT, Stripe, correo, Frontend, CORS y configuración de base de datos.
- Worker: Outbox y configuración explícita de base de datos. **No hereda ninguna conexión a BD del archivo común**; sigue fallando al iniciar si no tiene proveedor y conexión configurados.
- Web pública: servicios de alta y su propia conexión configurada en el entorno local.
- Web privada: por ahora solo interfaz, sin conexión de base de datos.
- MAUI: en el futuro solo URL de API y opciones públicas, nunca secretos ni acceso a bases de datos.

La antigua ruta `config/appsettings.Shared.json` se elimina del repositorio al mover el archivo; no es necesario conservarla.\n\nNo borrar `appsettings.Local.json`, `appsettings.Development.json`, `appsettings.Production.json`, User Secrets ni cadenas de conexión locales. Solo se eliminan del archivo base de API las secciones `Invitations` y `Subscriptions` que ahora proceden del común. Si una aplicación necesita un valor distinto, puede redefinirlo en su propio archivo.

## Seguridad

No incluir `ConnectionStrings`, contraseñas SMTP, claves JWT, Stripe ni certificados AEAT en `appsettings.Shared.json`. El contenido común se distribuye con los hosts; no se envía a MAUI. No se han ejecutado migraciones ni pruebas contra bases de datos.
