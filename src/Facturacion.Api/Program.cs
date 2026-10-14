using Facturacion.Api.Security;
using Facturacion.Application.DependencyInjection;
using Facturacion.Configuration;
using Facturacion.Configuration.Options;
using Facturacion.Infrastructure.Billing;
using Facturacion.Infrastructure.DependencyInjection;
using Facturacion.Infrastructure.Email;
using Facturacion.Infrastructure.Invitations;
using Facturacion.Infrastructure.Multitenancy;
using Facturacion.Infrastructure.Persistence;
using Facturacion.Infrastructure.Persistence.Providers;
using Facturacion.Verifactu.DependencyInjection;
using Microsoft.Extensions.Configuration;

const string FrontendCorsPolicy = "Frontend";

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddFacturacionConfiguration(builder.Environment, FacturacionHost.Api);

// La API necesita base de datos, firma de token e invitaciones. Stripe y Email
// son opcionales: sin ellos el alta y el correo quedan apagados, no el arranque.
builder.Services.AddFacturacionOptions(
    ConfigurationScope.Database | ConfigurationScope.Jwt | ConfigurationScope.Invitations);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Contratos tipados. Se leen una vez aqui porque AddCors y AddVerifactu exigen
// el valor en el momento del registro; el resto de la app usa IOptions<T>.
var cors = builder.Configuration.GetSection(CorsOptions.SectionName).Get<CorsOptions>()
           ?? new CorsOptions();
var fiscalSchema = builder.Configuration.GetSection(FiscalSchemaOptions.SectionName).Get<FiscalSchemaOptions>()
                   ?? new FiscalSchemaOptions();

// CORS. Sin origenes configurados no se permite ningun origen: un despliegue
// que olvida la lista no debe quedar abierto por defecto.
builder.Services.AddCors(options => options.AddPolicy(
    FrontendCorsPolicy,
    policy =>
    {
        if (cors.IsClosed)
        {
            policy.SetIsOriginAllowed(_ => false);
            return;
        }

        policy
            .WithOrigins(cors.AllowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    }));

// Base de datos. BillingDbContext y AuthDbContext comparten resolutor y cadenas.
builder.Services.AddConfiguredDatabaseContext<BillingDbContext>();

// Identity + JWT + Multi-tenant
builder.Services.AddSaasAuthentication();

// Application
builder.Services.AddVerifactu(fiscalSchema.SchemaDirectory);

builder.Services.AddBillingApplication();

// Infrastructure
builder.Services.AddBillingRepositories();

// Alta self-service + Stripe (inactivo si no hay seccion Stripe configurada)
builder.Services.AddSaasSelfService();

// Invitaciones entre usuarios del tenant
builder.Services.AddSaasInvitations();

// Canal de correo (verificacion de registro e invitaciones)
builder.Services.AddFacturacionEmail();

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