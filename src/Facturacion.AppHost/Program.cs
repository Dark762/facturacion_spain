using Aspire.Hosting;

// Aspire solo orquesta procesos locales; MySQL/MariaDB y Mailpit se gestionan fuera.
// El entorno seleccionado en launchSettings se propaga a todos los recursos.
var builder = DistributedApplication.CreateBuilder(args);
var environmentName = builder.Environment.EnvironmentName;

builder.AddProject<Projects.Facturacion_Api>("api")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", environmentName)
    .WithEnvironment("DOTNET_ENVIRONMENT", environmentName);

builder.AddProject<Projects.Facturacion_Web_Public>("web-public")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", environmentName)
    .WithEnvironment("DOTNET_ENVIRONMENT", environmentName);

builder.AddProject<Projects.Facturacion_Web_App>("web-app")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", environmentName)
    .WithEnvironment("DOTNET_ENVIRONMENT", environmentName);

builder.AddProject<Projects.Facturacion_Workers>("workers")
    .WithEnvironment("DOTNET_ENVIRONMENT", environmentName);

// Los servicios externos se conectan con la configuracion centralizada por host/entorno.
builder.Build().Run();
