# Arquitectura de interfaz (fase 1)

- **Web pública**: `Facturacion.Web.Public` — landing, precios y registro (sin llamadas HTTP aún).
- **Web privada**: `Facturacion.Web.App` — maqueta responsive **sin datos reales ni autenticación**. No desplegar como aplicación operativa hasta implementar login y autorización.
- **Componentes compartidos**: `Facturacion.UI.Shared` — Razor Class Library y sistema visual adaptable.
- **Contrato HTTP**: `Facturacion.Client` — rutas de API; servicios HTTP, DTOs y autenticación quedan para la fase 2.
- **Desktop y móvil**: futura aplicación .NET MAUI Blazor Hybrid para Windows, macOS, Android e iOS, reutilizando componentes de `Facturacion.UI.Shared`. No se crea aún un proyecto MAUI sin validar workloads y entornos de compilación.

## Seguridad y alcance

No se modifica el backend, la persistencia, Stripe ni VERI*FACTU. No hay pagos ni registro funcionales desde la interfaz todavía. Las páginas privadas actuales son una maqueta sin datos. Antes de habilitar funcionalidad privada deben existir autenticación y autorización por tenant y empresa, con validación en la API. Los estados Outbox y AEAT no son equivalentes.

## Ejecutar localmente

```sh
dotnet run --project src/Facturacion.Web.Public
dotnet run --project src/Facturacion.Web.App
```

Ejecutar en terminales separadas y configurar puertos diferentes si fuera necesario. El diseño usa CSS propio y no depende de servicios externos.

## Próximas fases

1. Conectar planes y registro a la API con DTOs compartidos.
2. Verificación de correo y checkout con redirecciones seguras.
3. Autenticación y autorización reales para web privada.
4. Clientes y componentes compartidos con MAUI Blazor Hybrid.
