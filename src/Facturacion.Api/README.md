# Facturacion.Api

API HTTP de **Facturación Spain**, desarrollada en ASP.NET Core / .NET 8. Expone casos de uso comerciales y de administración SaaS; **no significa que la emisión fiscal esté habilitada**.

## Responsabilidades

- Controladores HTTP y respuestas de error; Swagger en `Development`.
- ASP.NET Identity y validación de JWT (emisor, audiencia, firma, expiración y sello de seguridad del usuario).
- `TenantResolutionMiddleware` tras la autenticación y antes de la autorización.
- CORS mediante `Cors:AllowedOrigins`; sin orígenes definidos no se habilita ninguno.
- Registro de Application, Infrastructure, motor VERI*FACTU, correo, invitaciones y alta self-service.

## Endpoints implementados

| Ruta | Uso |
| --- | --- |
| `POST /api/auth/login` | Autenticación con correo, contraseña y tenant |
| `GET /api/auth/me/companies` | Empresas del usuario autenticado en su tenant |
| `POST /api/invoices` | Crear borrador |
| `GET /api/invoices/{invoiceId}` | Consultar factura |
| `POST /api/invoices/{invoiceId}/issue` | Solicitar emisión; actualmente bloqueada por componentes fiscales pendientes |
| `GET /api/saas/plans` | Consultar planes activos |
| `POST /api/saas/register` | Iniciar alta |
| `POST /api/saas/verify-email` | Verificar correo |
| `POST /api/saas/checkout` | Iniciar Checkout de suscripción |
| `POST /api/invitations` | Crear invitación |
| `POST /api/invitations/accept` | Aceptar invitación |
| `POST /api/tenant-admin/companies` | Crear empresa con autorización de administración |
| `PUT /api/tenant-admin/companies/{companyId}/users` | Asignar usuario a empresa |
| `POST /api/tenant-admin/users` | Retirado: devuelve HTTP 410 |

También se expone un controlador de webhooks Stripe: revisar su código y configuración antes de integrar un endpoint público. Los detalles de contratos, permisos y errores se encuentran en los controladores, no se debe deducir una API más amplia de la lista de interfaces.

## Configuración

Los valores de `appsettings.json` son plantillas, no secretos utilizables. Definir como mínimo `Database:Provider`, la cadena en `ConnectionStrings:Default`, y `Jwt:SigningKey` de al menos 32 bytes junto con `Jwt:Issuer` y `Jwt:Audience`. Proveedores configurados: `MySql`, `MariaDb`, `PostgreSql`, `SqlServer`; verificar nombre exacto y cadena para el entorno elegido.

El SMTP (`Email`), Stripe (`Stripe`), Frontend e invitaciones requieren una configuración coherente si se utilizan sus flujos. **No almacenar credenciales en Git.** Swagger solo se habilita en `Development`.

```bash
dotnet run --project src/Facturacion.Api/Facturacion.Api.csproj
```

## Seguridad y límites

El tenant debe resolverse desde el JWT y la pertenencia autorizada, nunca aceptar el `TenantId` del cliente como autoridad. Las reglas por empresa deben mantenerse en los servicios. El endpoint `issue` consulta la preparación del motor fiscal y no permite emitir mientras falten XML y XSD; no representa un sistema fiscal listo para producción. Ver [README principal](../../README.md).
