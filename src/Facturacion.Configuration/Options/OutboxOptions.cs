namespace Facturacion.Configuration.Options;

/// <summary>Cola de emision fiscal. Solo la consume el worker de outbox.</summary>
public sealed class OutboxOptions
{
    public const string SectionName = "Outbox";

    /// <summary>Cuantos mensajes se reclaman por vuelta.</summary>
    public int BatchSize { get; set; } = 25;

    /// <summary>Espera entre vueltas.</summary>
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Tiempo que un mensaje permanece reclamado. Si el worker muere a mitad,
    /// el mensaje vuelve a la cola al expirar la lease.
    /// </summary>
    public TimeSpan Lease { get; set; } = TimeSpan.FromMinutes(2);

    /// <summary>Intentos antes de marcar el mensaje como fallido.</summary>
    public int MaxAttempts { get; set; } = 5;

    /// <summary>Espera base del reintento; luego se duplica en cada intento.</summary>
    public TimeSpan BaseDelay { get; set; } = TimeSpan.FromSeconds(30);
}