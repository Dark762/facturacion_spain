using System.Text;
using Facturacion.Configuration.Options;
using Microsoft.Extensions.Options;

namespace Facturacion.Configuration.Validation;

/// <summary>
/// Firma de token. Obligatoria donde se emite: una clave debil o una vida
/// excesiva harian aceptable un token falsificado.
/// </summary>
public sealed class JwtOptionsValidator : IValidateOptions<JwtOptions>
{
    private const int MinimumKeyBytes = 32;

    public ValidateOptionsResult Validate(string? name, JwtOptions options)
    {
        var failures = new List<string>();
        var prefix = JwtOptions.SectionName;

        if (Encoding.UTF8.GetByteCount(options.SigningKey) < MinimumKeyBytes)
            failures.Add($"{prefix}:SigningKey debe tener al menos {MinimumKeyBytes} bytes UTF-8.");

        if (options.ExpiryMinutes is < 1 or > 60)
            failures.Add($"{prefix}:ExpiryMinutes debe estar entre 1 y 60.");

        if (string.IsNullOrWhiteSpace(options.Issuer))
            failures.Add($"{prefix}:Issuer es obligatorio.");

        if (string.IsNullOrWhiteSpace(options.Audience))
            failures.Add($"{prefix}:Audience es obligatorio.");

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}