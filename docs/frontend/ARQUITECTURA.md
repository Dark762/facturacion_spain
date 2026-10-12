# Arquitectura Blazor — acceso directo en web

La rama `develop` contiene dos aplicaciones web independientes y una biblioteca de componentes Razor compartida.

- `Facturacion.Web.Public`: Blazor Web App con renderizado interactivo en servidor. Configura Identity, SelfService y correo y utiliza servicios de servidor directamente; la página de planes consulta `SelfServiceDbContext` mediante `PublicPlansService` (sin peticiones HTTP internas).
- `Facturacion.Web.App`: maqueta privada, **todavía sin autenticación**. No publicar como aplicación de facturación real.
- `Facturacion.UI.Shared`: diseño y componentes reutilizables.
- `Facturacion.Client`: contrato y futuro cliente HTTP para MAUI.
- Windows, macOS, Android e iOS: futuro .NET MAUI Blazor Hybrid, comunicándose por HTTPS con la API existente. **Las aplicaciones nativas aún no están creadas**.

## Seguridad y siguientes pasos

La conexión a la base de datos y los secretos permanecen únicamente en el servidor. La web pública requiere configuración de base de datos, Identity y correo equivalentes a los servicios del backend; nunca se deben distribuir secretos a dispositivos. La página de planes no presenta precios porque el contrato actual solo contiene código y nombre. El registro y el pago siguen deshabilitados en la interfaz: antes de exponerlos, completar formularios, validación, antiabuso, confirmación de correo y protección frente a errores. La web privada requiere autenticación, autorización por tenant y gestión correcta de ámbitos EF Core antes de usar datos reales.

No se han ejecutado migraciones ni operaciones contra bases de datos. No se ha modificado el backend existente.

## Ejecución

```sh
dotnet run --project src/Facturacion.Web.Public
```

Usar un entorno de desarrollo aislado con conexión de prueba; no ejecutar contra bases de datos de producción. Verificar compilación y arranque en un entorno configurado antes de desplegar.
