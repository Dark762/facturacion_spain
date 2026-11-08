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


## Crear el usuario propietario (SuperAdmin) — MySQL/MariaDB

**No existe una columna ni un rol `SuperAdmin` en `AspNetUsers`.** La API concede acceso global exclusivamente cuando el `Id` de un usuario autenticado coincide con `PlatformAdmin:UserId`. Crear un usuario en la tabla **no lo convierte por sí solo en SuperAdmin**.

### Opción recomendada: crear la identidad con ASP.NET Core Identity

Crear el usuario mediante el registro normal o mediante un proceso administrativo que utilice `UserManager<ApplicationUser>.CreateAsync(user, password)`. Identity genera el `PasswordHash` y gestiona los sellos de seguridad. No insertar contraseñas en texto plano ni calcular hashes con SHA/MD5/BCrypt por cuenta propia.

Si **ya tienes un usuario creado** en `AspNetUsers`, no necesitas otro INSERT en esa tabla. Busca su ID:

```sql
SELECT Id, Email, IsActive, EmailConfirmed
FROM AspNetUsers
WHERE NormalizedEmail = UPPER('admin@ejemplo.com');
```

### INSERT de vinculación al tenant (si aún no existe)

El login actual requiere que el usuario pertenezca a un tenant activo. Sustituye los valores de ejemplo y comprueba que el tenant ya existe:

```sql
SET @admin_user_id = 'ID_REAL_DE_ASPNETUSERS';
SET @tenant_id = 'UUID_REAL_DE_UN_TENANT_EXISTENTE';

INSERT INTO TenantUsers (Id, TenantId, UserId, IsActive, CreatedAt)
SELECT UUID(), @tenant_id, @admin_user_id, 1, UTC_TIMESTAMP(6)
WHERE EXISTS (SELECT 1 FROM AspNetUsers WHERE Id = @admin_user_id AND IsActive = 1)
  AND EXISTS (SELECT 1 FROM Tenants WHERE Id = @tenant_id AND IsActive = 1)
  AND NOT EXISTS (
      SELECT 1 FROM TenantUsers WHERE TenantId = @tenant_id AND UserId = @admin_user_id
  );
```

Si ya existe una fila `TenantUsers` inactiva, este INSERT no la reactiva; revisa esa pertenencia explícitamente antes de iniciar sesión. La pertenencia al tenant **no requiere** asignar un rol Admin en `CompanyUsers` para el panel de plataforma.

### Configurar el propietario

Con el `Id` exacto devuelto por `AspNetUsers`, desde la raíz del repositorio:

```powershell
dotnet user-secrets set "PlatformAdmin:UserId" "ID_REAL_DE_ASPNETUSERS" --project src/Facturacion.Api
```

En producción usar la variable de entorno `PlatformAdmin__UserId`. El archivo `Hosts/Api/appsettings.json` mantiene `PlatformAdmin:UserId` vacío a propósito. Reinicia la API después de configurar el valor.

### Comprobar

```sql
SELECT u.Id, u.Email, u.IsActive AS UsuarioActivo,
       tu.TenantId, tu.IsActive AS PertenenciaActiva
FROM AspNetUsers u
LEFT JOIN TenantUsers tu ON tu.UserId = u.Id
WHERE u.Id = 'ID_REAL_DE_ASPNETUSERS';
```

Inicia sesión en Web.App con email, contraseña y el GUID del tenant asociado. Comprueba que `GET /api/platform/summary` devuelve 200 para el propietario y 403 para otros usuarios autenticados. El identificador del propietario **no es** el GUID del tenant ni el correo electrónico.

> **Si necesitas un INSERT directo en `AspNetUsers`**, primero genera un `PasswordHash` compatible usando `UserManager`/`PasswordHasher<ApplicationUser>` y comprueba el esquema de la migración Identity. Se desaconseja el INSERT manual porque es fácil crear una identidad que no pueda autenticarse o bloquearse correctamente.

## Validación
Probar 401 sin token, 403 con tenant-admin ajeno, 200 solo con propietario configurado y JWT válido. No se ha ejecutado build/test al escribir esta guía.
