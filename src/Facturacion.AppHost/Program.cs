using Aspire.Hosting;

// Aspire solo orquesta procesos locales; MySQL/MariaDB y Mailpit se gestionan fuera.
// El entorno seleccionado en launchSettings se propaga a todos los recursos.
var builder = DistributedApplication.CreateBuilder(args);
var environmentName = builder.Environment.EnvironmentName;
// Aspire ya proporciona un receptor OTLP y un panel local de métricas.
// Activar la exportación solo en desarrollo; producción se configura por host.
var enableLocalMetrics = builder.Environment.IsDevelopment() ||
    builder.Environment.IsEnvironment("Local");
var enableMetrics = enableLocalMetrics ? "true" : "false";

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
builder.Build().Run();
