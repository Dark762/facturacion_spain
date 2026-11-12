using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Facturacion.Infrastructure.Billing;

/// <summary>Persistencia best-effort: un fallo del almacén nunca oculta la excepción original.</summary>
public sealed class SystemIncidentRecorder(SelfServiceDbContext db, ILogger<SystemIncidentRecorder> logger)
{
    public async Task RecordAsync(string application, string operation, string code,
        string? traceId, Guid? tenantId, CancellationToken ct = default)
    {
        try
        {
            db.SystemIncidents.Add(new SystemIncident
            {
                Application = application,
                Operation = operation,
                ErrorCode = code,
                TraceId = traceId,
                TenantId = tenantId
            });
            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "No se pudo persistir incidencia operativa. ErrorCode {ErrorCode}, TraceId {TraceId}", code, traceId);
            db.ChangeTracker.Clear();
        }
    }
}
