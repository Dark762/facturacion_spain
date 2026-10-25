// Orquestacion local: utiliza la base MySQL/MariaDB y Mailpit ya instalados
// en Windows. No crea contenedores ni modifica datos existentes.
var builder = DistributedApplication.CreateBuilder(args);

// Cada host mantiene sus propios appsettings centralizados y User Secrets.
// Development evita el perfil "Local" historico de la API.
var api = builder.AddProject<Projects.Facturacion_Api>("api")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development");

var publicWeb = builder.AddProject<Projects.Facturacion_Web_Public>("web-public")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development");

var privateWeb = builder.AddProject<Projects.Facturacion_Web_App>("web-app")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development");

var workers = builder.AddProject<Projects.Facturacion_Workers>("workers")
    .WithEnvironment("DOTNET_ENVIRONMENT", "Development");

// No se usa WithReference a MySQL ni a Mailpit: son servicios externos
// accesibles desde localhost mediante los ajustes de Facturacion.Configuration.
builder.Build().Run();
