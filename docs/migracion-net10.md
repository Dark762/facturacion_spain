# Migración inicial a .NET 10

## Estado

Todos los proyectos de `src/` y `test/` de `Facturacion.slnx` apuntan a `net10.0`. En `Facturacion.AppHost`, `Aspire.AppHost.Sdk` y `Aspire.Hosting.AppHost` declaran ambos la versión `13.6.4`, para evitar el desajuste anterior entre SDK y hosting.

Esta primera fase **no actualiza EF Core 8, los proveedores Pomelo/Npgsql/SQL Server, Identity 8 ni otras dependencias**. Las bibliotecas .NET 8 pueden ser consumidas por una aplicación `net10.0`, pero hay que validar el restore y el comportamiento real; la actualización de los proveedores y de las migraciones de base de datos se hará de manera separada y controlada. Tampoco se han cambiado las reglas fiscales ni el comportamiento B2B.

## Requisitos locales

- SDK de .NET 10 instalado: `dotnet --list-sdks`.
- Visual Studio con soporte para .NET 10 y la versión de Aspire utilizada; verificar en el instalador y actualizar antes de abrir la solución.
- Acceso a NuGet para restaurar el SDK y paquete de Aspire `13.6.4`. Si NuGet no encuentra esa versión, comprobar el feed y la versión disponible antes de cambiarla.
- MySQL/MariaDB y Mailpit continúan siendo servicios externos a AppHost.

## Validación en Windows

```powershell
dotnet --info
dotnet restore Facturacion.slnx
dotnet build Facturacion.slnx --no-restore
dotnet test Facturacion.slnx --no-build
dotnet run --project src/Facturacion.AppHost/Facturacion.AppHost.csproj --launch-profile Development
```

Para un error de generación del namespace `Projects` de Aspire en Visual Studio, cerrar Visual Studio, borrar los directorios locales `.vs`, `src/Facturacion.AppHost/bin` y `src/Facturacion.AppHost/obj`, restaurar y compilar de nuevo. No crear el namespace `Projects` manualmente.

## Pendiente

- Confirmar restore, build, tests y arranque en Visual Studio y CLI con el SDK 10.
- Revisar advertencias de compatibilidad y actualizar EF Core/proveedores en una fase posterior, con validación de migraciones y bases de datos soportadas.
- Comprobar compatibilidad de despliegues, Docker, CI y entornos de producción antes de publicar.
