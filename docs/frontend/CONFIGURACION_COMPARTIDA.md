# Configuración transversal (develop)

`src/Facturacion.Configuration/appsettings.Shared.json` contiene exclusivamente valores comunes **no sensibles**. Se copia como `appsettings.Shared.json` al directorio de contenido/salida de API, Workers y ambas webs. `Facturacion.Configuration` añade esta fuente **con la prioridad más baja**; por tanto prevalecen `appsettings.json`, `appsettings.{Environment}.json`, User Secrets, variables de entorno y argumentos del host.

## Qué se mantiene por aplicación

- API: JWT, Stripe, correo, Frontend, CORS y configuración de base de datos.
- Worker: Outbox y configuración explícita de base de datos. **No hereda ninguna conexión a BD del archivo común**; sigue fallando al iniciar si no tiene proveedor y conexión configurados.
- Web pública: servicios de alta y su propia conexión configurada en el entorno local.
- Web privada: por ahora solo interfaz, sin conexión de base de datos.
- MAUI: en el futuro solo URL de API y opciones públicas, nunca secretos ni acceso a bases de datos.

Para la web pública, `appsettings.Development.json` define `Database:Provider=MySql` y `Database:ConnectionStringName=Default`. Crea localmente (sin subir a Git) `src/Facturacion.Web.Public/appsettings.Development.local.json` con la cadena real:

```json
{
  "ConnectionStrings": {
    "Default": "Server=localhost;Port=3306;Database=TU_BD;User=TU_USUARIO;Password=TU_PASSWORD"
  }
}
```

La web solo carga este archivo opcional en `Development`; variables de entorno y argumentos tienen prioridad. El patrón `appsettings.*.local.json` ya está en `.gitignore`. No se añade ninguna conexión por defecto para evitar acceder accidentalmente a una base distinta.

La antigua ruta `config/appsettings.Shared.json` se elimina del repositorio al mover el archivo; no es necesario conservarla.\n\nNo borrar `appsettings.Local.json`, `appsettings.Development.json`, `appsettings.Production.json`, User Secrets ni cadenas de conexión locales. Solo se eliminan del archivo base de API las secciones `Invitations` y `Subscriptions` que ahora proceden del común. Si una aplicación necesita un valor distinto, puede redefinirlo en su propio archivo.

## Seguridad

No incluir `ConnectionStrings`, contraseñas SMTP, claves JWT, Stripe ni certificados AEAT en `appsettings.Shared.json`. El contenido común se distribuye con los hosts; no se envía a MAUI. No se han ejecutado migraciones ni pruebas contra bases de datos.
