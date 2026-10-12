using System.Net;
using System.Net.Mail;
using Facturacion.Application.Billing;
using Facturacion.Application.Invitations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Facturacion.Infrastructure.Email;

/// <summary>
/// Envio real por SMTP. Cubre tanto la verificacion de correo del alta self-service
/// como el enlace de invitacion, que comparten el mismo canal de salida.
/// </summary>
public sealed class SmtpEmailSender(
    IOptions<EmailOptions> options, ILogger<SmtpEmailSender> logger)
    : IRegistrationEmailSender, IInvitationEmailSender
{
    private readonly EmailOptions _options = options.Value;

    public Task SendConfirmationAsync(
        string email, Guid registrationId, string token, CancellationToken ct) =>
        SendAsync(email, "Verifica tu correo electronico",
            """
            <p>Bienvenido a Facturacion.</p>
            <p>Confirma esta direccion de correo para activar tu cuenta:</p>
            <p><a href="{0}">Verificar correo</a></p>
            <p>El enlace caduca por seguridad si no se usa a tiempo.</p>
            """, token, ct);

    public Task SendInvitationAsync(
        string email, string invitationUrl, DateTime expiresAtUtc, CancellationToken ct) =>
        SendAsync(email, "Te han invitado a colaborar",
            """
            <p>Te han invitado a colaborar en una empresa de Facturacion.</p>
            <p><a href="{0}">Aceptar invitacion</a></p>
            <p>La invitacion caduca el {1:yyyy-MM-dd HH:mm} UTC.</p>
            """, invitationUrl, expiresAtUtc.ToString("u"), ct);

    private async Task SendAsync(string to, string subject, string htmlTemplate, params object[] args)
    {
        if (!string.IsNullOrWhiteSpace(_options.Smtp.Host))
        {
            using var client = new SmtpClient(_options.Smtp.Host, _options.Smtp.Port)
            {
                EnableSsl = _options.Smtp.EnableSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network
            };

            if (!string.IsNullOrWhiteSpace(_options.Smtp.Username))
                client.Credentials = new NetworkCredential(
                    _options.Smtp.Username, _options.Smtp.Password);

            using var message = new MailMessage
            {
                From = new MailAddress(_options.FromAddress, _options.FromName),
                Subject = subject,
                Body = string.Format(htmlTemplate.Replace("{1}", "{1}"), args),
                IsBodyHtml = true
            };
            message.To.Add(to);

            await client.SendMailAsync(message);
            logger.LogInformation("Correo enviado a {Email}: {Subject}", to, subject);
            return;
        }

        // Sin SMTP configurado no se puede entregar el correo: fallar es preferible a
        // dar por buena una verificacion que el usuario nunca recibira.
        logger.LogError(
            "Email:Smtp:Host no configurado; no se pudo enviar '{Subject}' a {Email}.",
            subject, to);

        throw new InvalidOperationException(
            "El envio de correo no esta configurado. Define Email:Smtp:Host.");
    }
}