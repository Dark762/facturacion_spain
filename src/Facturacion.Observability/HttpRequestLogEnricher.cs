using Microsoft.AspNetCore.Http;
using Serilog.Core;
using Serilog.Events;

namespace Facturacion.Observability;

/// <summary>Enriquece cada evento HTTP sin confiar en cabeceras o claims de tenant sin validar.</summary>
public sealed class HttpRequestLogEnricher : ILogEventEnricher
{
    public const string ValidatedTenantIdKey = "Facturacion.Observability.ValidatedTenantId";
    public const string ValidatedUserIdKey = "Facturacion.Observability.ValidatedUserId";
    public const string ValidatedCompanyIdKey = "Facturacion.Observability.ValidatedCompanyId";

    private readonly IHttpContextAccessor _accessor;

    public HttpRequestLogEnricher(IHttpContextAccessor accessor) => _accessor = accessor;

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory factory)
    {
        var context = _accessor.HttpContext;
        if (context is null) return;

        logEvent.AddPropertyIfAbsent(factory.CreateProperty("TraceId", context.TraceIdentifier));
        logEvent.AddPropertyIfAbsent(factory.CreateProperty("RequestPath", context.Request.Path.Value ?? ""));
        AddValidated(logEvent, factory, context, ValidatedTenantIdKey, "TenantId");
        AddValidated(logEvent, factory, context, ValidatedUserIdKey, "UserId");
        AddValidated(logEvent, factory, context, ValidatedCompanyIdKey, "CompanyId");
    }

    private static void AddValidated(LogEvent logEvent, ILogEventPropertyFactory factory,
        HttpContext context, string key, string propertyName)
    {
        if (context.Items.TryGetValue(key, out var value) &&
            value is string text && !string.IsNullOrWhiteSpace(text))
            logEvent.AddPropertyIfAbsent(factory.CreateProperty(propertyName, text));
    }
}
