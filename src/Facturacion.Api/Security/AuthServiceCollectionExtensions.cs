using System.Security.Claims;
using System.Text;
using Facturacion.Api.Security;
using Facturacion.Application.Interfaces;
using Facturacion.Infrastructure.Identity;
using Facturacion.Infrastructure.Multitenancy;
using Facturacion.Infrastructure.Persistence.Providers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
namespace Microsoft.Extensions.DependencyInjection;
public static class AuthServiceCollectionExtensions
{
    public static IServiceCollection AddSaasAuthentication(this IServiceCollection services, IConfiguration config)
    {
        var jwt = config.GetSection(JwtOptions.Section).Get<JwtOptions>()
            ?? throw new InvalidOperationException("Falta configuración Jwt.");

        if (Encoding.UTF8.GetByteCount(jwt.SigningKey) < 32 || jwt.ExpiryMinutes is < 1 or > 60 ||
            string.IsNullOrWhiteSpace(jwt.Issuer) || string.IsNullOrWhiteSpace(jwt.Audience))
            throw new InvalidOperationException("Configuración JWT inválida. Usa una clave secreta de al menos 32 bytes.");

        services.Configure<JwtOptions>(config.GetSection(JwtOptions.Section));

        services.AddConfiguredDatabaseContext<AuthDbContext>(config);

        services.AddIdentityCore<ApplicationUser>(options =>
        {
            options.Password.RequiredLength = 12;
            options.Password.RequireDigit = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.User.RequireUniqueEmail = true;
        }).AddSignInManager().AddEntityFrameworkStores<AuthDbContext>().AddDefaultTokenProviders();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = ClaimTypes.NameIdentifier
                };
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async context =>
                    {
                        var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
                        var id = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                        var stamp = context.Principal?.FindFirst("security_stamp")?.Value;
                        var user = id is null ? null : await userManager.FindByIdAsync(id);
                        if (user is null || !user.IsActive || user.SecurityStamp != stamp)
                            context.Fail("Usuario deshabilitado o sesión revocada.");
                    }
                };
            });
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
