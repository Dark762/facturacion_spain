using Facturacion.Application.Invitations;
using Microsoft.Extensions.DependencyInjection;

namespace Facturacion.Infrastructure.Invitations;

public static class InvitationRegistration
{
    public static IServiceCollection AddSaasInvitations(this IServiceCollection services)
    {
        // IInvitationEmailSender lo registra AddFacturacionEmail: ambos contratos
        // comparten el mismo emisor SMTP.
        services.AddScoped<IUserInvitationService, UserInvitationService>();
        return services;
    }
}