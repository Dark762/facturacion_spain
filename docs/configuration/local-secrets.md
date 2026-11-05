# Configuración local y secretos de FacturaciónES

## Fuentes y prioridad
Los JSON versionados viven en `src/Facturacion.Configuration/Shared` y `Hosts/<host>`. El sistema carga configuración compartida, compartida por entorno, host y host por entorno. Los secretos de usuario y las variables de entorno sobrescriben estos valores. **No versionar secretos reales.**

La API carga User Secrets tanto en `Development` como en `Local` (el perfil HTTPS de Visual Studio usa `Local`). Los demás hosts usan User Secrets automáticamente en `Development`. `Web.App` tiene su propio `UserSecretsId`.

**Importante:** `ConnectionStrings:Default` ya no contiene la contraseña `root/root` en Git. Si esa contraseña se utilizó fuera de un entorno desechable, rotarla. No copiarla a otros entornos.

## Archivos de configuración (no sensibles)

| Archivo | Claves |
| --- | --- |
| `Shared/appsettings.json` | `Database:Provider`, `Database:ConnectionStringName`, `ConnectionStrings:Default` vacío, `Invitations:ExpirationHours`, `Identity:Policy:*`, `Fiscal:SchemaDirectory`, `Logging:LogLevel:*` |
| `Hosts/Api/appsettings.json` | `PlatformAdmin:UserId` vacío, `Jwt:Issuer`, `Jwt:Audience`, `Jwt:ExpiryMinutes`, `Jwt:SigningKey` vacío, `Cors:AllowedOrigins`, `Invitations:AcceptUrl`, `Stripe:SecretKey` vacío, `Stripe:WebhookSecret` vacío, `Stripe:SuccessUrl`, `Stripe:CancelUrl`, `Email:*`, `AllowedHosts` |
| `Hosts/Api/appsettings.Local.json` | `Database:Provider=MariaDb`, JWT local (excepto firma), URLs de desarrollo, SMTP Mailpit |
| `Hosts/Api/appsettings.Development.json` | URLs de desarrollo, SMTP Mailpit y niveles de log |
| `Hosts/Web.App/appsettings.json` | `Facturacion:ApiBaseUrl` vacío |
| `Hosts/Web.App/appsettings.Development.json` | `Facturacion:ApiBaseUrl=https://localhost:7003` (ajustar si Aspire asigna otro puerto) |
| `Hosts/Web.Public/appsettings.Development.json` | URLs de Stripe y Web.App, SMTP Mailpit |
| `Hosts/Workers/appsettings.json` | `Outbox:BatchSize`, `Outbox:PollInterval`, `Outbox:Lease`, `Outbox:MaxAttempts`, `Outbox:BaseDelay` |

Los valores de ejemplo `tudominio.com` y URLs de localhost deben reemplazarse por URLs reales según el entorno. Las claves de Stripe, SMTP y JWT nunca se guardan en esos JSON.

## User Secrets por proyecto (desarrollo local)

Ejecutar desde la raíz del repositorio; reemplazar los ejemplos por valores reales, sin publicar su salida.

### API — necesarios para arrancar y administrar la plataforma
```powershell
dotnet user-secrets set "ConnectionStrings:Default" "Server=localhost;Port=3306;Database=facturacion_saas;User=TU_USUARIO;Password=TU_PASSWORD" --project src/Facturacion.Api
dotnet user-secrets set "Jwt:SigningKey" "CLAVE_ALEATORIA_LARGA_Y_UNICA_DE_AL_MENOS_32_CARACTERES" --project src/Facturacion.Api
dotnet user-secrets set "PlatformAdmin:UserId" "ID_DE_ASPNETUSERS_DEL_PROPIETARIO" --project src/Facturacion.Api
```
`PlatformAdmin:UserId` debe ser el ID de `AspNetUsers`, no email ni TenantId. El propietario necesita además pertenecer a un tenant activo para utilizar el login actual. La firma JWT debe ser aleatoria y confidencial.

### API — opcionales según funcionalidades
```powershell
dotnet user-secrets set "Stripe:SecretKey" "sk_test_..." --project src/Facturacion.Api
dotnet user-secrets set "Stripe:WebhookSecret" "whsec_..." --project src/Facturacion.Api
dotnet user-secrets set "Email:Smtp:Username" "USUARIO_SMTP" --project src/Facturacion.Api
dotnet user-secrets set "Email:Smtp:Password" "PASSWORD_SMTP" --project src/Facturacion.Api
```
Configurar **ambas** claves Stripe juntas si se activa Stripe; los valores `sk_test_` y `whsec_` son ejemplos, no claves válidas. Mailpit local no suele requerir usuario ni contraseña SMTP, por lo que esas dos claves son innecesarias con Mailpit.

### Web.Public — registro, correo y pagos
```powershell
dotnet user-secrets set "ConnectionStrings:Default" "Server=localhost;Port=3306;Database=facturacion_saas;User=TU_USUARIO;Password=TU_PASSWORD" --project src/Facturacion.Web.Public
dotnet user-secrets set "Stripe:SecretKey" "sk_test_..." --project src/Facturacion.Web.Public
dotnet user-secrets set "Stripe:WebhookSecret" "whsec_..." --project src/Facturacion.Web.Public
```
Web.Public procesa el webhook de Stripe y necesita su propia configuración si se usa ese flujo. En Mailpit no es necesario secreto SMTP; con un proveedor SMTP real, añadir `Email:Smtp:Username` y `Email:Smtp:Password` en Web.Public.

### Workers — cola de facturación
```powershell
dotnet user-secrets set "ConnectionStrings:Default" "Server=localhost;Port=3306;Database=facturacion_saas;User=TU_USUARIO;Password=TU_PASSWORD" --project src/Facturacion.Workers
```

### Web.App — sesión de usuario y panel privado
```powershell
dotnet user-secrets set "Facturacion:ApiBaseUrl" "https://localhost:7003" --project src/Facturacion.Web.App
```
Esta URL **no es un secreto**; es un override local cuando el puerto de Aspire difiere del puerto fijo. Web.App no necesita ni debe recibir la firma JWT ni las credenciales MySQL. Para desplegar Web.App, configurar `Facturacion__ApiBaseUrl` en el servidor.

## Variables de entorno en producción
Usar doble guion bajo en lugar de dos puntos, por ejemplo:
- API: `ConnectionStrings__Default`, `Jwt__SigningKey`, `PlatformAdmin__UserId`, `Stripe__SecretKey`, `Stripe__WebhookSecret`, `Email__Smtp__Username`, `Email__Smtp__Password`.
- Web.Public: `ConnectionStrings__Default` y las claves Stripe/SMTP que use.
- Workers: `ConnectionStrings__Default`.
- Web.App: `Facturacion__ApiBaseUrl`.

Opcionales de observabilidad por host: `FacturacionLogging__MinimumLevel`, `FacturacionLogging__EnableFile`, `FacturacionLogging__FilePath`, `FacturacionLogging__RetentionDays`, `FacturacionMetrics__EnableOtlp`, `FacturacionMetrics__OtlpEndpoint`. Son ajustes operativos, no secretos salvo que una URL incluya credenciales (evitarlo).

## Comprobación local
```powershell
dotnet user-secrets list --project src/Facturacion.Api
dotnet user-secrets list --project src/Facturacion.Web.Public
dotnet user-secrets list --project src/Facturacion.Workers
dotnet user-secrets list --project src/Facturacion.Web.App
```
**Atención:** `list` muestra valores sensibles en pantalla. No compartir capturas ni pegar su salida. Los secretos de usuario no están cifrados: son adecuados para desarrollo, no para producción.

**Nota de entorno:** en el perfil API `Local`, los User Secrets se cargan expresamente. Los perfiles de Web.Public, Web.App y Workers deben ejecutarse en `Development` para usar User Secrets automáticamente. Si se cambian a `Local`, configurar las credenciales por variables de entorno o extender su carga explícita de secretos.
