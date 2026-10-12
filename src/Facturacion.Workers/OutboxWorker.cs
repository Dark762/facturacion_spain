namespace Facturacion.Workers;

/// <summary>
/// Bucle del procesador de la cola.
///
/// No es un BackgroundService mas que duerme: cada vuelta reclama mensajes,
/// los procesa y, si no habia nada, espera. Si el procesado lanza, el bucle
/// sobrevive: un fallo puntual no debe tumbar el worker entero.
/// </summary>
public sealed class OutboxWorker : BackgroundService
{
    private readonly FiscalOutboxProcessor _processor;
    private readonly OutboxOptions _options;
    private readonly ILogger<OutboxWorker> _logger;

    public OutboxWorker(
        FiscalOutboxProcessor processor,
        OutboxOptions options,
        ILogger<OutboxWorker> logger)
    {
        _processor = processor;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Worker de outbox iniciado: lote {BatchSize}, intentos {MaxAttempts}",
            _options.BatchSize, _options.MaxAttempts);

        using var timer = new PeriodicTimer(_options.PollInterval);

        // La primera vuelta es inmediata: no hay nada que esperar todavia.
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var processed = await _processor.ProcessBatchAsync(stoppingToken);

                if (processed > 0)
                {
                    _logger.LogInformation("Procesados {Count} mensajes del outbox", processed);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                // Sin esto, una excepcion fugada termina el BackgroundService
                // y el proceso se queda vivo sin procesar nada.
                _logger.LogError(ex, "Fallo en la vuelta del worker de outbox");
            }

            try
            {
                if (!await timer.WaitForNextTickAsync(stoppingToken))
                {
                    break;
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("Worker de outbox detenido.");
    }
}