using System.Diagnostics.Metrics;

namespace Facturacion.Observability;

/// <summary>Métricas agregables; etiquetas de cardinalidad acotada, sin identificadores de clientes.</summary>
public static class FacturacionMetrics
{
    public const string MeterName = "Facturacion.Operations";
    private static readonly Meter Meter = new(MeterName, "1.0.0");
    private static readonly Counter<long> Completed = Meter.CreateCounter<long>(
        "facturacion.operation.completed", unit: "{operation}", description: "Operaciones finalizadas por resultado");
    private static readonly Histogram<double> Duration = Meter.CreateHistogram<double>(
        "facturacion.operation.duration", unit: "s", description: "Duración de operaciones críticas");

    public static void Record(string operation, string outcome, double elapsedSeconds)
    {
        var tags = new TagList
        {
            { "operation", operation },
            { "outcome", outcome }
        };
        Completed.Add(1, tags);
        Duration.Record(elapsedSeconds, tags);
    }
}
