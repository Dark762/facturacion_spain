using Facturacion.Configuration;
using Facturacion.Application.Interfaces;
using Facturacion.Infrastructure.Billing;
using Facturacion.Infrastructure.DependencyInjection;
using Facturacion.Infrastructure.Multitenancy;
using Facturacion.Infrastructure.Persistence;
using Facturacion.Infrastructure.Persistence.Providers;
using Facturacion.Verifactu.DependencyInjection;
using Facturacion.Workers;
using Microsoft.Extensions.Options;

var builder = Host.CreateApplicationBuilder(args);
builder.Configuration.AddFacturacionConfiguration(builder.Environment, "Workers");

// El worker no lleva base de datos por defecto. Arrancar sin configuracion
// contra una base que no es la suya seria peor que no arrancar: el worker
// vaciaria una cola ajena. Se falla aqui, con un mensaje util, en lugar de
// dejar un proceso vivo que parece trabajar.
var provider = builder.Configuration["Database:Provider"];
var connectionName = builder.Configuration["Database:ConnectionStringName"] ?? "Default";
var connectionString = builder.Configuration.GetConnectionString(connectionName);

if (string.IsNullOrWhiteSpace(provider) || string.IsNullOrWhiteSpace(connectionString))
{
    Console.Error.WriteLine(
        "El worker de outbox no arranca sin base de datos configurada.\n" +
        "Define Database:Provider (MariaDb, MySql, PostgreSql o SqlServer) y " +
        "ConnectionStrings:<nombre>. El motivo es no procesar por error una cola ajena.");

    return;
}

builder.Services.AddConfiguredDatabaseContext<BillingDbContext>(builder.Configuration);
builder.Services.AddBillingRepositories();

// Un worker recorre empresas de varios clientes, asi que necesita poder fijar
// el tenant. Sin esto, BillingDbContext no puede ni construir el filtro.
builder.Services.AddScoped<TenantContext>();
builder.Services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<TenantContext>());

// Validacion del XML fiscal contra los XSD oficiales de la AEAT.
builder.Services.AddVerifactu();

builder.Services.Configure<OutboxOptions>(builder.Configuration.GetSection(OutboxOptions.SectionName));
builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<OutboxOptions>>().Value);

builder.Services.AddSingleton<FiscalOutboxProcessor>();
builder.Services.AddHostedService<OutboxWorker>();

var host = builder.Build();
host.Run();