using Facturacion.Application.Interfaces;
using Facturacion.Configuration;
using Facturacion.Configuration.Options;
using Facturacion.Infrastructure.Billing;
using Facturacion.Infrastructure.DependencyInjection;
using Facturacion.Infrastructure.Multitenancy;
using Facturacion.Infrastructure.Persistence;
using Facturacion.Infrastructure.Persistence.Providers;
using Facturacion.Verifactu.DependencyInjection;
using Facturacion.Workers;
using Microsoft.Extensions.Configuration;

var builder = Host.CreateApplicationBuilder(args);
builder.Configuration.AddFacturacionConfiguration(builder.Environment, FacturacionHost.Workers);

// El worker recorre empresas de varios clientes: sin base de datos arrancaria
// vaciando una cola ajena, y sin esquema fiscal validaria XML que Hacienda
// rechaza. Ambos se validan al arrancar y paran el proceso con el nombre de
// la clave, en vez de dejarlo vivo fingiendo que trabaja.
builder.Services.AddFacturacionOptions(
    ConfigurationScope.Database | ConfigurationScope.Outbox);

var fiscalSchema = builder.Configuration.GetSection(FiscalSchemaOptions.SectionName).Get<FiscalSchemaOptions>()
                   ?? new FiscalSchemaOptions();

builder.Services.AddConfiguredDatabaseContext<BillingDbContext>();
builder.Services.AddBillingRepositories();

// Un worker recorre empresas de varios clientes, asi que necesita poder fijar
// el tenant. Sin esto, BillingDbContext no puede ni construir el filtro.
builder.Services.AddScoped<TenantContext>();
builder.Services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<TenantContext>());

// Validacion del XML fiscal contra los XSD oficiales de la AEAT.
builder.Services.AddVerifactu(fiscalSchema.SchemaDirectory);

builder.Services.AddSingleton<FiscalOutboxProcessor>();
builder.Services.AddHostedService<OutboxWorker>();

var host = builder.Build();
host.Run();