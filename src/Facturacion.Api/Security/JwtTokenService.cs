using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Facturacion.Infrastructure.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
namespace Facturacion.Api.Security;
public sealed class JwtTokenService
{
    private readonly JwtOptions _options;
    public JwtTokenService(IOptions<JwtOptions> options) => _options = options.Value;
    public (string Token, DateTime ExpiresUtc) Create(ApplicationUser user, Guid tenantId)
    {
        var now = DateTime.UtcNow;
        var expiry = now.AddMinutes(_options.ExpiryMinutes);
        var claims = new[] {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new Claim("tenant_id", tenantId.ToString("D")),
            new Claim("security_stamp", user.SecurityStamp ?? "")
        };
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var jwt = new JwtSecurityToken(_options.Issuer, _options.Audience, claims,
            notBefore: now, expires: expiry,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return (new JwtSecurityTokenHandler().WriteToken(jwt), expiry);
    }
}
