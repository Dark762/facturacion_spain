using System.Net;
using System.Net.Mail;
using Facturacion.Application.Billing;
using Facturacion.Application.Invitations;
using Facturacion.Configuration.Options;
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
        SendAsync(email, "Tu código de verificación de FacturaciónES",
            """
            <p>Bienvenido a FacturaciónES.</p>
            <p>Introduce este código de seis dígitos en el paso 2 del registro:</p>
            <p style="font-size:30px;font-weight:800;letter-spacing:6px;color:#25324a">{0}</p>
            <p>El código caduca en pocos minutos. Si no has solicitado este registro, ignora el mensaje.</p>
            <p>Por seguridad, no compartas este código.</p>
            """, ct, token);

    public Task SendInvitationAsync(
        string email, string invitationUrl, DateTime expiresAtUtc, CancellationToken ct) =>
        SendAsync(email, "Te han invitado a colaborar",
            """
            <p>Te han invitado a colaborar en una empresa de Facturacion.</p>
            <p><a href="{0}">Aceptar invitacion</a></p>
            <p>La invitacion caduca el {1:yyyy-MM-dd HH:mm} UTC.</p>
            """, ct, invitationUrl, expiresAtUtc.ToString("u"));

    private async Task SendAsync(string to, string subject, string htmlTemplate, CancellationToken ct, params object[] args)
    {
        if (!string.IsNullOrWhiteSpace(_options.Smtp.Host))
        {
            try
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

            await client.SendMailAsync(message, ct);
            logger.LogInformation("Correo entregado al servidor SMTP. Tipo {Subject}", subject);
            return;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (SmtpException ex)
            {
                var transient = ex.StatusCode is SmtpStatusCode.ServiceNotAvailable or SmtpStatusCode.MailboxBusy or SmtpStatusCode.LocalErrorInProcessing or SmtpStatusCode.InsufficientStorage or SmtpStatusCode.TransactionFailed;
                throw new EmailDeliveryException(transient ? "EMAIL_SMTP_TEMPORARY" : "EMAIL_SMTP_REJECTED", transient, ex);
            }
            catch (Exception ex) when (ex is System.IO.IOException or System.Net.Sockets.SocketException or TimeoutException)
            {
                throw new EmailDeliveryException("EMAIL_TRANSPORT_UNAVAILABLE", true, ex);
            }
        }

        // Sin SMTP configurado no se puede entregar el correo: fallar es preferible a
        // dar por buena una verificacion que el usuario nunca recibira.
        throw new EmailDeliveryException("EMAIL_NOT_CONFIGURED", false);
    }
}