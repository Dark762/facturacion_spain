# Configuración centralizada por aplicación

Los archivos base están en `src/Facturacion.Configuration/`:

- `appsettings.Shared.json`: valores compartidos.
- `appsettings.Api.json`: configuración base de API (sin credenciales).
- `appsettings.Web.Public.json`: proveedor MySql y nombre de conexión para la web pública.
- `appsettings.Web.App.json`: configuración de la web privada, actualmente vacía.
- `appsettings.Workers.json`: opciones Outbox y logging; **sin conexión a BD por defecto**.

Cada host copia y carga exclusivamente `Shared` y su archivo específico. Los ajustes por entorno de cada host, variables y argumentos tienen prioridad.

## Desarrollo local de la web pública

Mantén en tu equipo `src/Facturacion.Web.Public/appsettings.Development.local.json`, excluido por `.gitignore`:

```json
{
  "ConnectionStrings": {
    "Default": "Server=localhost;Port=3306;Database=TU_BD;User=TU_USUARIO;Password=TU_PASSWORD"
  }
}
```

No subas credenciales a GitHub. Los archivos de entorno de API y Workers siguen en sus proyectos y **no deben borrarse**. La API y Workers no comparten credenciales ni cadenas de conexión con la web. No se han ejecutado migraciones ni pruebas sobre bases de datos.
