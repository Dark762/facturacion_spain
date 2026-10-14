using Facturacion.Configuration.Options;
using Microsoft.Extensions.Options;

namespace Facturacion.Configuration.Validation;

/// <summary>
/// Invitaciones. Obligatoria: un enlace sin destino, o sin HTTPS, o con una
/// vigencia absurda produce invitaciones que nadie puede usar.
/// </summary>
public sealed class InvitationOptionsValidator : IValidateOptions<InvitationOptions>
{
    private const int MinHours = 1;
    private const int MaxHours = 720;

    public ValidateOptionsResult Validate(string? name, InvitationOptions options)
    {
        var failures = new List<string>();
        var prefix = InvitationOptions.SectionName;

        if (options.ExpirationHours is < MinHours or > MaxHours)
            failures.Add($"{prefix}:ExpirationHours debe estar entre {MinHours} y {MaxHours}.");

        if (string.IsNullOrWhiteSpace(options.AcceptUrl))
        {
            failures.Add($"{prefix}:AcceptUrl es obligatorio.");
        }
        else if (!Uri.TryCreate(options.AcceptUrl, UriKind.Absolute, out var parsed))
        {
            failures.Add($"{prefix}:AcceptUrl debe ser una URL absoluta.");
        }
        else if (parsed.Scheme != Uri.UriSchemeHttps)
        {
            failures.Add($"{prefix}:AcceptUrl debe usar HTTPS.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}