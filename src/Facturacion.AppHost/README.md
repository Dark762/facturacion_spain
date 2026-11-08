# Facturacion.AppHost

## Responsabilidad
Orquestación local con .NET Aspire. Inicia cuatro recursos: `api`, `web-public`, `web-app` y `workers`. Las bibliotecas Domain, Application, Infrastructure, Configuration y Observability no aparecen como recursos ejecutables.

## Configuración
`Properties/launchSettings.json` define perfiles y puertos del dashboard. `Program.cs` propaga el entorno ASPNETCORE/DOTNET a los procesos y activa `FacturacionMetrics__EnableOtlp` en Development/Local. Aspire proporciona el endpoint OTLP cuando corresponde.

## Dependencias externas
MySQL/MariaDB y Mailpit se ejecutan fuera de Aspire en esta solución. Cada proceso carga su configuración centralizada y sus secretos propios. No guardar contraseñas en AppHost ni duplicar cadenas de conexión aquí.

## Uso
Arrancar `Facturacion.AppHost` desde Visual Studio. Comprobar recursos Running y abrir Metrics después de ejecutar operaciones de registro/provisioning. El dashboard de Aspire no constituye monitorización histórica de producción.

## Atención
Si Aspire asigna puertos dinámicos, verificar que `Facturacion:ApiBaseUrl` de Web.App apunta al endpoint HTTPS real de la API.
