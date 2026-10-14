using System.Security.Claims;
using System.Text;
using Facturacion.Api.Security;
using Facturacion.Application.Interfaces;
using Facturacion.Configuration.Options;
using Facturacion.Infrastructure.Identity;
using Facturacion.Infrastructure.Multitenancy;
using Facturacion.Infrastructure.Persistence.Providers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.Extensions.DependencyInjection;

public static class AuthServiceCollectionExtensions
{
    /// <summary>
    /// Identity, JWT y multi-tenancy.
    ///
    /// No se lee ni se valida configuracion aqui: JwtOptions llega ya validado
    /// por el validador central al arrancar. La politica de contrasenas es la
    /// misma que aplica la web publica, para que no divergan.
    /// </summary>
    public static IServiceCollection AddSaasAuthentication(this IServiceCollection services)
    {
        services.AddConfiguredDatabaseContext<AuthDbContext>();

        services.AddSingleton<IConfigureOptions<IdentityOptions>, ConfigureIdentityPolicy>();

        services.AddIdentityCore<ApplicationUser>()
            .AddSignInManager()
            .AddEntityFrameworkStores<AuthDbContext>()
            .AddDefaultTokenProviders();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();
        services.AddSingleton<IConfigureOptions<JwtBearerOptions>, ConfigureJwtBearer>();

        services.AddAuthorization();
        services.AddHttpContextAccessor();
        services.AddScoped<TenantContext>();
        services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<TenantContext>());
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<ICompanyAuthorizationService, CompanyAuthorizationService>();
        services.AddScoped<JwtTokenService>();

        return services;
    }
}

/// <summary>
/// Proyecta JwtOptions ya validado sobre el middleware. La clave se lee del
/// contenedor, no de un cierre sobre IConfiguration, para que recargar la
/// configuracion con reloadOnChange tenga efecto.
/// </summary>
internal sealed class ConfigureJwtBearer(IOptions<JwtOptions> jwt)
    : IConfigureOptions<JwtBearerOptions>
{
    public void Configure(JwtBearerOptions options) => Configure(Options.Options.DefaultName, options);

    public void Configure(string? name, JwtBearerOptions options)
    {
        var value = jwt.Value;

        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = value.Issuer,
            ValidateAudience = true,
            ValidAudience = value.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(value.SigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = ClaimTypes.NameIdentifier
        };
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var userManager = context.HttpContext.RequestServices
                    .GetRequiredService<UserManager<ApplicationUser>>();
                var id = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                var stamp = context.Principal?.FindFirst("security_stamp")?.Value;
                var user = id is null ? null : await userManager.FindByIdAsync(id);
                if (user is null || !user.IsActive || user.SecurityStamp != stamp)
                    context.Fail("Usuario deshabilitado o sesión revocada.");
            }
        };
    }
}

/// <summary>
/// Politica de contrasenas y bloqueo compartida con la web publica. Si
/// divergieran, un usuario podria validar una contrasena en un sitio que el
/// otro rechaza.
/// </summary>
internal sealed class ConfigureIdentityPolicy(IOptions<IdentityPolicyOptions> policy)
    : IConfigureOptions<IdentityOptions>
{
    public void Configure(IdentityOptions options)
    {
        var value = policy.Value;

        options.Password.RequiredLength = value.RequiredLength;
        options.Password.RequireDigit = value.RequireDigit;
        options.Password.RequireUppercase = value.RequireUppercase;
        options.Password.RequireNonAlphanumeric = value.RequireNonAlphanumeric;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(value.LockoutMinutes);
        options.Lockout.MaxFailedAccessAttempts = value.MaxFailedAccessAttempts;
        options.User.RequireUniqueEmail = value.RequireUniqueEmail;
    }
}