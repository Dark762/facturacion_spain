namespace Facturacion.Infrastructure.Email;

public sealed class EmailOptions
{
    public const string Section = "Email";

    public string Provider { get; set; } = "Smtp";
    public string FromAddress { get; set; } = "";
    public string FromName { get; set; } = "Facturacion SaaS";
    public SmtpOptions Smtp { get; set; } = new();
}

public sealed class SmtpOptions
{
    public string Host { get; set; } = "";
    public int Port { get; set; } = 587;
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public bool EnableSsl { get; set; } = true;
}