# Aspire en desarrollo local (sin Docker)

El proyecto `src/Facturacion.AppHost` orquesta `Api`, `Web.Public`, `Web.App` y `Workers` desde Aspire. **No inicia MySQL/MariaDB ni Mailpit**: deben estar funcionando en Windows antes de arrancar.

## Requisitos
- SDK .NET 8 compatible y cargas de trabajo de Aspire disponibles para tu versión de Visual Studio.
- MySQL/MariaDB local con la base de datos existente.
- Mailpit escuchando en `localhost:1025` (interfaz web `http://localhost:8025`).
- Stripe CLI independiente cuando pruebes pagos.

## Inicio
1. `git checkout develop && git pull origin develop`
2. `dotnet restore Facturacion.slnx`
3. `dotnet build Facturacion.slnx`
4. Inicia MySQL/MariaDB y Mailpit en Windows.
5. Configura `Hosts/Web.Public/appsettings.Development.json` y los User Secrets de `Facturacion.Web.Public` para Stripe.
6. En Visual Studio, establece **Facturacion.AppHost** como proyecto de inicio y ejecuta su perfil HTTPS. También puedes probar `dotnet run --project src/Facturacion.AppHost --launch-profile https`.

Los proyectos siguen leyendo la configuración centralizada en `Facturacion.Configuration/Shared` y `Facturacion.Configuration/Hosts`. El AppHost no almacena secretos ni cambia cadenas de conexión.

**Atención:** los perfiles actuales de la web pública usan puertos distintos: el perfil Project usa `https://localhost:65288` y IIS Express usa `https://localhost:44314`. Al ejecutar Aspire no se usa IIS Express; actualiza `Stripe:SuccessUrl`, `Stripe:CancelUrl` y el destino de `stripe listen --forward-to` según la URL HTTPS que Aspire muestre para `web-public`.

La primera integración aporta orquestación y logs de procesos. Las trazas distribuidas, métricas y health checks de aplicación requieren una fase adicional de instrumentación de los cuatro hosts. No se ha validado la compilación en un entorno Windows real.
