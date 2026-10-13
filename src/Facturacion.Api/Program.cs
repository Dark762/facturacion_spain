using Facturacion.Api.Security;
using Facturacion.Application.DependencyInjection;
using Facturacion.Verifactu.DependencyInjection;
using Facturacion.Infrastructure.Billing;
using Facturacion.Infrastructure.DependencyInjection;
using Facturacion.Infrastructure.Email;
using Facturacion.Infrastructure.Invitations;
using Facturacion.Infrastructure.Multitenancy;
using Facturacion.Infrastructure.Persistence;
using Facturacion.Infrastructure.Persistence.Providers;

const string FrontendCorsPolicy = "Frontend";

var builder = WebApplication.CreateBuilder(args);

// Controllers
builder.Services.AddControllers();

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS. Sin orígenes configurados no se permite ningun origen.
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? Array.Empty<string>();

builder.Services.AddCors(options => options.AddPolicy(
    FrontendCorsPolicy,
    policy =>
    {
        if (allowedOrigins.Length == 0)
        {
            policy.SetIsOriginAllowed(_ => false);
            return;
        }

        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    }));

// Base de datos. BillingDbContext y AuthDbContext comparten resolutor y cadenas.
builder.Services.AddConfiguredDatabaseContext<BillingDbContext>(
    builder.Configuration);

// Identity + JWT + Multi-tenant
builder.Services.AddSaasAuthentication(builder.Configuration);

// Application
builder.Services.AddVerifactu();

builder.Services.AddBillingApplication();

// Infrastructure
builder.Services.AddBillingRepositories();

// Alta self-service + Stripe (inactivo si no hay sección Stripe configurada)
builder.Services.AddSaasSelfService(builder.Configuration);

// Invitaciones entre usuarios del tenant
builder.Services.AddSaasInvitations();

// Canal de correo (verificación de registro e invitaciones)
builder.Services.AddFacturacionEmail(builder.Configuration);

builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

// Configure the HTTP request pipeline.
// Swagger tambien en Local, para no perder la documentacion al cambiar el
// entorno por defecto: antes, arrancar en local significaba Development y con
// ello la carga de user-secrets con la base de datos real.
if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Local"))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseExceptionHandler();

app.UseCors(FrontendCorsPolicy);

// Primero validamos el JWT.
app.UseAuthentication();

// Despues resolvemos el tenant autorizado.
app.UseMiddleware<TenantResolutionMiddleware>();

// La autorizacion se evalua con el tenant ya resuelto.
app.UseAuthorization();

// ==============================================
// ENDPOINTS
// ==============================================

app.MapControllers();


app.Run();

// Required so that WebApplicationFactory<Program> can reference the entry point.
public partial class Program { }
