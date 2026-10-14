# Configuración centralizada por aplicación

Toda la configuración tipada vive en `Facturacion.Configuration`, que es el
único sitio donde se declara qué claves existen, cuáles son obligatorias y cómo
se validan. Cada host hace dos llamadas y nada más:

```csharp
builder.Configuration.AddFacturacionConfiguration(builder.Environment, FacturacionHost.Api);
builder.Services.AddFacturacionOptions(ConfigurationScope.Database | ConfigurationScope.Jwt);
```

## Orden de prioridad

De mayor a menor:

1. Variables de entorno y argumentos de línea de comandos
2. `appsettings.{Entorno}.local.json` (solo el host que lo declara, solo en Development)
3. `appsettings.Development.json` / `.Production.json` de cada host
4. `appsettings.{Host}.json` de `Facturacion.Configuration`
5. `appsettings.Shared.json` de `Facturacion.Configuration`

Los archivos base están en `src/Facturacion.Configuration/`:

- `appsettings.Shared.json`: base de datos, invitaciones, política de Identity,
  esquemas fiscales y logging. Los carga **todos** los hosts.
- `appsettings.Api.json`: JWT, CORS, invitaciones, Stripe, Email.
- `appsettings.Web.Public.json`: vacío a propósito; hereda de Shared.
- `appsettings.Web.App.json`: vacío; la web privada no habla con la base de datos.
- `appsettings.Workers.json`: opciones de la cola de outbox.

## Qué es obligatorio y qué no

`ConfigurationScope` declara qué secciones deben existir en cada host.

**Obligatorias — si faltan, el proceso muere al arrancar nombrando la clave:**

- `Database`: en Api, Workers y Web.Public. Sin ella no hay nada que hacer.
- `Jwt`: solo Api.
- `Outbox`: solo Workers.
- `Invitations`: solo Api.

**Opcionales — si faltan, la función queda apagada con un aviso y la app arranca:**

- `Stripe`: sin claves, el alta self-service se desactiva. Con **una sola** de
  las dos claves sí es error: es un despliegue a medias y dejaría el cobro
  medio activo.
- `Email`: sin configurar, los envíos fallan de forma explícita en vez de fingir
  una entrega.

## Desarrollo local de la web pública

`appsettings.Development.local.json` está excluido por `.gitignore`. Crea
`src/Facturacion.Web.Public/appsettings.Development.local.json`:

```json
{
  "Database": {
    "Provider": "MariaDb"
  },
  "ConnectionStrings": {
    "Default": "Server=localhost;Port=3306;Database=TU_BD;User=TU_USUARIO;Password=TU_PASSWORD"
  }
}
```

La clave debe llamarse `Default` porque `appsettings.Shared.json` fija
`Database:ConnectionStringName = "Default"`.

Usa `"MariaDb"` y no `"MySql"`: ambos proveedores usan `ServerVersion.AutoDetect`,
pero el de MariaDB existe para que Pomelo genere DDL válido en MariaDB.

> `ServerVersion.AutoDetect` abre una conexión al arrancar. Si la base no está
> levantada, la aplicación falla en el bootstrap.

Los archivos de entorno de API y Workers siguen en sus proyectos y **no deben
borrarse**. La API y Workers no comparten credenciales con la web.

## Fuera de Development

`appsettings.*.local.json` **solo se carga en Development**. En servidor la
cadena de conexión llega por variable de entorno:

```
ConnectionStrings__Default=Server=...;Database=...;User=...;Password=...
```

Los archivos de `Facturacion.Configuration` están versionados: nunca deben
contener una contraseña.

## Notas de despliegue

- **Data Protection**: Web.Public registra `AddDataProtection()` porque lo exige
  `DataProtectorTokenProvider` (lo aporta `AddAuthentication`, que aquí no se
  llama). Con varias instancias, las claves se generan por instancia y los
  tokens de verificación emitidos por una no los lee otra. Fija un almacén
  compartido y `SetApplicationName` constante.
- **No se han ejecutado migraciones ni pruebas sobre bases de datos reales.**