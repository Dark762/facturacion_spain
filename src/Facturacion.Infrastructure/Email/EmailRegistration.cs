using Facturacion.Application.Billing;
using Facturacion.Application.Invitations;
using Microsoft.Extensions.DependencyInjection;

namespace Facturacion.Infrastructure.Email;

public static class EmailRegistration
{
    /// <summary>
    /// Registra el canal de salida de correo. Sin configuracion la aplicacion
    /// arranca igual, pero cualquier envio falla de forma explicita en lugar de
    /// simular una entrega que el usuario nunca recibio. La comprobacion vive
    /// en EmailOptionsValidator, no aqui.
    /// </summary>
    public static IServiceCollection AddFacturacionEmail(this IServiceCollection services)
    {
        services.AddSingleton<SmtpEmailSender>();
        services.AddSingleton<IRegistrationEmailSender>(
            sp => sp.GetRequiredService<SmtpEmailSender>());
        services.AddSingleton<IInvitationEmailSender>(
            sp => sp.GetRequiredService<SmtpEmailSender>());

        return services;
    }
}