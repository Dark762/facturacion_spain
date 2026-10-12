namespace Facturacion.Application.Billing;
public interface IRegistrationEmailSender
{
    Task SendConfirmationAsync(string email, Guid registrationId, string token, CancellationToken ct);
}
