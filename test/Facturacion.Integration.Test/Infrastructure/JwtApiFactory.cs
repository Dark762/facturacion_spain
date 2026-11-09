namespace Facturacion.Integration.Test.Infrastructure;

/// <summary>
/// Variante de <see cref="ApiFactory"/> que conserva el esquema JwtBearer real.
/// Necesaria para ejercitar /api/auth/login/start y /api/auth/login/complete: si
/// se sustituye la autenticacion, el 401 del middleware nunca se reproduce.
/// </summary>
public sealed class JwtApiFactory : ApiFactory
{
    protected override bool UseTestAuthentication => false;
}
