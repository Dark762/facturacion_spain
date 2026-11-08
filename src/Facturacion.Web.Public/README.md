# Facturacion.Web.Public

## Responsabilidad
Portal público de planes, registro SaaS, verificación de correo y alta fiscal. Gestiona Stripe Checkout y recibe webhook de Stripe en el host público.

## Configuración
Archivos: `../Facturacion.Configuration/Hosts/Web.Public/appsettings.json` y variante de entorno, además de Shared.
- Obligatorio para persistencia: `ConnectionStrings:Default` (User Secrets/entorno).
- `Facturacion:WebAppUrl`: destino de la aplicación privada tras alta.
- `Stripe:SecretKey` y `Stripe:WebhookSecret`: necesarias juntas cuando se usan pagos.
- `Stripe:SuccessUrl` y `Stripe:CancelUrl`: URLs públicas del flujo de pago.
- `Email:Provider`, `Email:FromAddress`, `Email:Smtp:Host`, `Email:Smtp:Port`, `Email:Smtp:EnableSsl`; usuario y contraseña SMTP si procede.

## Desarrollo
```powershell
dotnet user-secrets set "ConnectionStrings:Default" "CADENA_MYSQL_LOCAL" --project src/Facturacion.Web.Public
dotnet user-secrets set "Stripe:SecretKey" "sk_test_..." --project src/Facturacion.Web.Public
dotnet user-secrets set "Stripe:WebhookSecret" "whsec_..." --project src/Facturacion.Web.Public
```
Mailpit local usa normalmente `localhost:1025` sin credenciales. El perfil Development carga User Secrets; Web.Public también admite `appsettings.Development.local.json` no versionado.

## Seguridad y operación
No provisionar por visitar la URL de éxito de Stripe; validar firma del webhook. No guardar secretos en HTML ni logs. Revisar que URLs de Checkout coincidan con los puertos/host reales y que la entrega de correo funcione.
