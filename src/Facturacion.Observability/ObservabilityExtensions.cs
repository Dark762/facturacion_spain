using OpenTelemetry;
using OpenTelemetry.Metrics;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;

namespace Facturacion.Observability;

/// <summary>Logging estructurado portable: consola JSON siempre, archivo opcional.</summary>
public static class ObservabilityExtensions
{
    public static IServiceCollection AddFacturacionObservability(
        this IServiceCollection services, IConfiguration configuration, string applicationName, string environmentName)
    {
        services.AddHttpContextAccessor();
        services.AddOpenTelemetry().WithMetrics(builder =>
        {
            builder.AddMeter(FacturacionMetrics.MeterName);
            if (bool.TryParse(configuration["FacturacionMetrics:EnableOtlp"], out var enabled) && enabled)
                builder.AddOtlpExporter(options =>
                {
                    var endpoint = configuration["FacturacionMetrics:OtlpEndpoint"];
                    // Sin endpoint explícito se usa OTEL_EXPORTER_OTLP_ENDPOINT,
                    // proporcionado automáticamente por Aspire a sus proyectos.
                    if (!string.IsNullOrWhiteSpace(endpoint))
                    {
                        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) ||
                            uri.Scheme is not ("http" or "https"))
                            throw new InvalidOperationException("FacturacionMetrics:OtlpEndpoint debe ser una URL HTTP(S) absoluta.");
                        options.Endpoint = uri;
                    }
                });
        });
        services.AddSerilog((serviceProvider, logger) =>
        {
            var section = configuration.GetSection("FacturacionLogging");
            var levelText = section["MinimumLevel"] ?? (environmentName == "Development" ? "Debug" : "Information");
            if (!Enum.TryParse<LogEventLevel>(levelText, true, out var level))
                level = LogEventLevel.Information;

            logger.MinimumLevel.Is(level)
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Error)
                .Enrich.FromLogContext()
                .Enrich.With(new HttpRequestLogEnricher(serviceProvider.GetRequiredService<IHttpContextAccessor>()))
                .Enrich.WithProperty("Application", applicationName)
                .Enrich.WithProperty("Environment", environmentName)
                .WriteTo.Console(new JsonFormatter(renderMessage: true));

            if (bool.TryParse(section["EnableFile"], out var enableFile) && enableFile)
            {
                var path = section["FilePath"];
                if (string.IsNullOrWhiteSpace(path))
                    throw new InvalidOperationException("FacturacionLogging:FilePath es obligatorio si EnableFile=true.");

                var retention = int.TryParse(section["RetentionDays"], out var days)
                    ? Math.Clamp(days, 1, 365) : 30;
                logger.WriteTo.File(new JsonFormatter(renderMessage: true), path,
                    rollingInterval: RollingInterval.Day, retainedFileCountLimit: retention,
                    fileSizeLimitBytes: 50 * 1024 * 1024, rollOnFileSizeLimit: true,
                    shared: false);
            }
        });
        return services;
    }
}
