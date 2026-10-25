using Aspire.Hosting;

// Orquestacion local: utiliza la base MySQL/MariaDB y Mailpit ya instalados
// en Windows. No crea contenedores ni modifica datos existentes.
var builder = DistributedApplication.CreateBuilder(args);

// Cada host mantiene sus propios appsettings centralizados y User Secrets.
// Development evita el perfil "Local" historico de la API.
var api = builder.AddProject("api", "../Facturacion.Api/Facturacion.Api.csproj")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development");

var publicWeb = builder.AddProject("web-public", "../Facturacion.Web.Public/Facturacion.Web.Public.csproj")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development");

var privateWeb = builder.AddProject("web-app", "../Facturacion.Web.App/Facturacion.Web.App.csproj")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development");

var workers = builder.AddProject("workers", "../Facturacion.Workers/Facturacion.Workers.csproj")
    .WithEnvironment("DOTNET_ENVIRONMENT", "Development");

// No se usa WithReference a MySQL ni a Mailpit: son servicios externos
// accesibles desde localhost mediante los ajustes de Facturacion.Configuration.
builder.Build().Run();
