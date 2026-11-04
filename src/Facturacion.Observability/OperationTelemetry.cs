using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Facturacion.Observability;

/// <summary>Medición de duración de operaciones sin registrar datos sensibles.</summary>
public static class OperationTelemetry
{
    public static async Task<T> MeasureAsync<T>(
        ILogger logger, string operation, Guid registrationId,
        Func<Task<T>> action, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var result = await action();
            stopwatch.Stop();
            logger.LogInformation(
                "Operación {Operation} completada. RegistrationId {RegistrationId}, DurationMs {DurationMs}, Outcome {Outcome}",
                operation, registrationId, stopwatch.ElapsedMilliseconds, "Success");
            return result;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            stopwatch.Stop();
            logger.LogInformation(
                "Operación {Operation} cancelada. RegistrationId {RegistrationId}, DurationMs {DurationMs}, Outcome {Outcome}",
                operation, registrationId, stopwatch.ElapsedMilliseconds, "Cancelled");
            throw;
        }
        catch (Exception)
        {
            stopwatch.Stop();
            logger.LogWarning(
                "Operación {Operation} fallida. RegistrationId {RegistrationId}, DurationMs {DurationMs}, Outcome {Outcome}",
                operation, registrationId, stopwatch.ElapsedMilliseconds, "Failure");
            throw;
        }
    }

    public static Task MeasureAsync(
        ILogger logger, string operation, Guid registrationId,
        Func<Task> action, CancellationToken cancellationToken = default) =>
        MeasureAsync(logger, operation, registrationId, async () =>
        {
            await action();
            return true;
        }, cancellationToken);
}
