# Guía de arquitectura y configuración — FacturaciónES

Solución .NET 8 multi-tenant. La solución se abre con `Facturacion.slnx`. La configuración versionada se centraliza en `src/Facturacion.Configuration` y los secretos se suministran por host.

## Mapa de proyectos

| Proyecto | Responsabilidad | README |
| --- | --- | --- |
| Domain | Entidades y reglas de dominio | [Leer](../../src/Facturacion.Domain/README.md) |
| Application | Casos de uso, DTO e interfaces | [Leer](../../src/Facturacion.Application/README.md) |
| Infrastructure | EF Core, Identity, multitenancy y servicios externos | [Leer](../../src/Facturacion.Infrastructure/README.md) |
| Configuration | JSON por host/entorno, opciones y validación | [Leer](../../src/Facturacion.Configuration/README.md) |
| Api | JWT, controladores, autorización y SuperAdmin | [Leer](../../src/Facturacion.Api/README.md) |
| Client | Clientes HTTP tipados | [Leer](../../src/Facturacion.Client/README.md) |
| UI.Shared | Componentes y estilos Blazor compartidos | [Leer](../../src/Facturacion.UI.Shared/README.md) |
| Web.App | Aplicación privada y centro de control | [Leer](../../src/Facturacion.Web.App/README.md) |
| Web.Public | Portal público y registro SaaS | [Leer](../../src/Facturacion.Web.Public/README.md) |
| Workers | Procesamiento asíncrono Outbox | [Leer](../../src/Facturacion.Workers/README.md) |
| AppHost | Orquestación local Aspire | [Leer](../../src/Facturacion.AppHost/README.md) |
| Observability | Serilog y métricas OpenTelemetry | [Leer](../../src/Facturacion.Observability/README.md) |
| Payments.Stripe | Integración técnica Stripe | [Leer](../../src/Facturacion.Payments.Stripe/README.md) |
| B2B | Contratos de intercambio B2B | [Leer](../../src/Facturacion.B2B/README.md) |
| Verifactu | Validación y contratos fiscales | [Leer](../../src/Facturacion.Verifactu/README.md) |

## Configuración esencial para empezar

1. Preparar MySQL/MariaDB y Mailpit (servicios externos al AppHost).
2. Configurar `ConnectionStrings:Default` en User Secrets de API, Web.Public y Workers.
3. Configurar `Jwt:SigningKey` en User Secrets de API.
4. Para centro de control, configurar `PlatformAdmin:UserId` en User Secrets de API con el ID de `AspNetUsers`.
5. Configurar `Facturacion:ApiBaseUrl` de Web.App con el puerto real de API.
6. Añadir credenciales Stripe solo cuando se utilicen pagos y SMTP autenticado solo si procede.

Consultar [comandos exactos de User Secrets](../configuration/local-secrets.md).

## Reglas de seguridad

- Nunca versionar contraseñas, firmas JWT, secretos Stripe ni cadenas de conexión con credenciales.
- No confundir rol Admin de empresa con `PlatformAdmin`.
- Las consultas de negocio se filtran por tenant; las globales requieren autorización específica de plataforma.
- No usar el dashboard Aspire como almacén histórico de métricas de producción.

## Verificación

```powershell
dotnet restore Facturacion.slnx
dotnet build Facturacion.slnx
dotnet test Facturacion.slnx
```

Los README son documentación de configuración y arquitectura; no certifican que los flujos se hayan compilado o probado. Actualizar esta guía y el README del proyecto correspondiente cuando se introduzca una clave, servicio o responsabilidad nueva.
