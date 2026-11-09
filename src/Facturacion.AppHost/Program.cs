using Aspire.Hosting;
using Microsoft.Extensions.Hosting;
using System.Text;

// Aspire solo orquesta procesos locales; MySQL/MariaDB y Mailpit se gestionan fuera.
// El entorno seleccionado en launchSettings se propaga a todos los recursos.
// Diagnóstico temprano: también funciona antes de que Aspire configure su logging.
// Archivo en %TEMP%, fuera del repositorio. No incluir configuración ni secretos.
var diagnosticPath = Path.Combine(Path.GetTempPath(), "facturacion-apphost-startup.log");
void StartupLog(string message)
{
    var line = $"[{DateTimeOffset.Now:O}] [AppHost PID {Environment.ProcessId}] {message}";
    Console.Error.WriteLine(line);
    try
    {
        File.AppendAllText(diagnosticPath, line + Environment.NewLine, Encoding.UTF8);
    }
    catch (IOException) { /* El diagnóstico nunca debe impedir el arranque. */ }
    catch (UnauthorizedAccessException) { /* La consola sigue disponible. */ }
}

try
{
StartupLog("INICIO: proceso AppHost iniciado.");
StartupLog("FASE 1: creando DistributedApplicationBuilder.");
var builder = DistributedApplication.CreateBuilder(args);
StartupLog("FASE 1 OK: builder creado.");
var environmentName = builder.Environment.EnvironmentName;
// Aspire ya proporciona un receptor OTLP y un panel local de métricas.
// Activar la exportación solo en desarrollo; producción se configura por host.
var enableLocalMetrics = builder.Environment.IsDevelopment() ||
    builder.Environment.IsEnvironment("Local");
var enableMetrics = enableLocalMetrics ? "true" : "false";

StartupLog("FASE 2: registrando api, web-public, web-app y workers.");
builder.AddProject<Projects.Facturacion_Api>("api")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", environmentName)
    .WithEnvironment("DOTNET_ENVIRONMENT", environmentName)
    .WithEnvironment("FacturacionMetrics__EnableOtlp", enableMetrics);

builder.AddProject<Projects.Facturacion_Web_Public>("web-public")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", environmentName)
    .WithEnvironment("DOTNET_ENVIRONMENT", environmentName)
    .WithEnvironment("FacturacionMetrics__EnableOtlp", enableMetrics);

builder.AddProject<Projects.Facturacion_Web_App>("web-app")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", environmentName)
    .WithEnvironment("DOTNET_ENVIRONMENT", environmentName)
    .WithEnvironment("FacturacionMetrics__EnableOtlp", enableMetrics);

builder.AddProject<Projects.Facturacion_Workers>("workers")
    .WithEnvironment("DOTNET_ENVIRONMENT", environmentName)
    .WithEnvironment("FacturacionMetrics__EnableOtlp", enableMetrics);

// Los servicios externos se conectan con la configuracion centralizada por host/entorno.
StartupLog("FASE 2 OK: recursos registrados.");
StartupLog("FASE 3: construyendo host.");
using var app = builder.Build();
StartupLog("FASE 3 OK: host construido.");
StartupLog("FASE 4: iniciando Aspire (Run).");
app.Run();
StartupLog("FIN: Aspire terminó su ejecución.");
}
catch (Exception ex)
{
    // La excepción completa permite identificar el fallo sin volcar variables ni secretos.
    StartupLog($"ERROR: {ex.GetType().FullName}: {ex.Message}");
    StartupLog($"STACK TRACE: {ex.StackTrace}");
    throw;
}
