namespace Facturacion.Configuration.Options;

/// <summary>
/// Canal de correo saliente. Opcional: sin configuracion los envios fallan de
/// forma explicita en lugar de fingir una entrega que nadie recibio.
/// </summary>
public sealed class EmailOptions
{
    public const string SectionName = "Email";

    public string Provider { get; set; } = "Smtp";
    public string FromAddress { get; set; } = "";
    public string FromName { get; set; } = "Facturacion SaaS";
    public SmtpOptions Smtp { get; set; } = new();

    /// <summary>Hay algo que enviar y adonde.</summary>
    public bool IsConfigured => !string.IsNullOrWhiteSpace(FromAddress);
}

public sealed class SmtpOptions
{
    public string Host { get; set; } = "";
    public int Port { get; set; } = 587;
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public bool EnableSsl { get; set; } = true;
}