using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace Facturacion.Api.Security;

/// <summary>
/// Identidad de plataforma configurada exclusivamente por el operador del servidor.
/// No utiliza los roles Admin de empresas ni acepta IDs desde peticiones públicas.
/// </summary>
public sealed class PlatformAdminRequirement : IAuthorizationRequirement { }

public sealed class PlatformAdminHandler(IConfiguration configuration)
    : AuthorizationHandler<PlatformAdminRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context, PlatformAdminRequirement requirement)
    {
        var configuredId = configuration["PlatformAdmin:UserId"];
        var authenticatedId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (context.User.Identity?.IsAuthenticated == true &&
            !string.IsNullOrWhiteSpace(configuredId) &&
            !string.IsNullOrWhiteSpace(authenticatedId) &&
            string.Equals(configuredId, authenticatedId, StringComparison.Ordinal))
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}
