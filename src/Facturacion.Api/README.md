# Facturacion.Api

## Responsabilidad
API HTTP de facturación y administración. Autentica con JWT, valida pertenencia al tenant, aplica autorización, expone controladores y respuestas uniformes `ApiResponse<T>`.

## Configuración
Archivos: `../Facturacion.Configuration/Hosts/Api/appsettings.json` y `appsettings.{Environment}.json`, más `Shared`.
- Obligatorios: `ConnectionStrings:Default`, `Jwt:SigningKey` y configuración JWT (Issuer, Audience, ExpiryMinutes).
- SuperAdmin: `PlatformAdmin:UserId` (ID exacto de `AspNetUsers`); vacío = acceso denegado.
- Opcionales por funcionalidad: `Stripe:SecretKey`, `Stripe:WebhookSecret`, `Email:Smtp:Username`, `Email:Smtp:Password`.
- No secretos: `Cors:AllowedOrigins`, `Invitations:AcceptUrl`, `Stripe:SuccessUrl`, `Stripe:CancelUrl`, `Email:FromAddress`.

## Desarrollo
El perfil HTTPS usa `Local` y puerto `https://localhost:7003`; User Secrets se cargan también en Local. Ejemplo:
```powershell
dotnet user-secrets set "Jwt:SigningKey" "CLAVE_ALEATORIA_LARGA" --project src/Facturacion.Api
dotnet user-secrets set "PlatformAdmin:UserId" "ID_ASPNETUSERS" --project src/Facturacion.Api
```
Ver [lista completa](../../docs/configuration/local-secrets.md).

## Seguridad
`TenantResolutionMiddleware` comprueba pertenencia activa al tenant del JWT. `PlatformAdmin` es una política adicional exclusiva del propietario, no equivale al rol Admin de empresa. Los endpoints `GET /api/platform/summary` y `GET /api/platform/tenants` están protegidos. No permitir lecturas globales desde endpoints ordinarios.

## Validación
Probar 401 sin token, 403 con tenant-admin ajeno, 200 solo con propietario configurado y JWT válido. No se ha ejecutado build/test al escribir esta guía.
