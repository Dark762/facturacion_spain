using Facturacion.Application.Billing;
using Facturacion.Application.Invitations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Facturacion.Infrastructure.Email;

public static class EmailRegistration
{
    /// <summary>
    /// Registra el canal de salida de correo. Sin la seccion "Email" la aplicacion
    /// arranca igual, pero cualquier envio falla de forma explicita en lugar de
    /// simular una entrega que el usuario nunca recibio.
    /// </summary>
    public static IServiceCollection AddFacturacionEmail(
        this IServiceCollection services, IConfiguration config)
    {
        var section = config.GetSection(EmailOptions.Section);

        if (section.Exists())
        {
            var email = section.Get<EmailOptions>()
                ?? throw new InvalidOperationException("Configuración Email inválida.");

            if (string.IsNullOrWhiteSpace(email.FromAddress))
                throw new InvalidOperationException(
                    "Configuración Email inválida. Define Email:FromAddress.");
            if (email.Provider.Equals("Smtp", StringComparison.OrdinalIgnoreCase) &&
                string.IsNullOrWhiteSpace(email.Smtp.Host))
                throw new InvalidOperationException(
                    "Configuración Email inválida. Define Email:Smtp:Host para el proveedor Smtp.");
            if (email.Smtp.Port is < 1 or > 65535)
                throw new InvalidOperationException(
                    "Configuración Email inválida. Email:Smtp:Port fuera de rango.");
        }

        services.Configure<EmailOptions>(section);
        services.AddSingleton<SmtpEmailSender>();
        services.AddSingleton<IRegistrationEmailSender>(
            sp => sp.GetRequiredService<SmtpEmailSender>());
        services.AddSingleton<IInvitationEmailSender>(
            sp => sp.GetRequiredService<SmtpEmailSender>());

        return services;
    }
}