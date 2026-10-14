namespace Facturacion.Configuration;

/// <summary>
/// Que secciones de configuracion son obligatorias para un host.
///
/// Lo obligatorio se valida al arrancar: si falta, el proceso muere con el
/// nombre de la clave. Lo opcional se apaga con un aviso y el resto de la
/// aplicacion sigue en pie. La distincion es deliberada: es lo que permite
/// levantar la web en local sin montar ni Stripe ni SMTP, sin abrir la puerta
/// a arrancar sin base de datos.
/// </summary>
[Flags]
public enum ConfigurationScope
{
    None = 0,

    /// <summary>Sin base de datos no hay nada que hacer. Obligatoria siempre.</summary>
    Database = 1 << 0,

    /// <summary>Sin firma de token no hay sesion. Solo en la API.</summary>
    Jwt = 1 << 1,

    /// <summary>Solo el worker procesa la cola de emision.</summary>
    Outbox = 1 << 2,

    /// <summary>Sin destino de invitacion el enlace no lleva a ninguna parte.</summary>
    Invitations = 1 << 3,

    All = Database | Jwt | Outbox | Invitations
}