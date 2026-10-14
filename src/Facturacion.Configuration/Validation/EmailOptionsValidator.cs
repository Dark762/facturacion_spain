using Facturacion.Configuration.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Facturacion.Configuration.Validation;

/// <summary>
/// Email. Opcional: sin configuracion los envios fallan de forma explicita
/// en lugar de fingir una entrega. Si esta configurado a medias, en cambio, es
/// un error: el remitente caeria en el vacio.
/// </summary>
public sealed class EmailOptionsValidator(ILogger<EmailOptionsValidator> logger) : IValidateOptions<EmailOptions>
{
    public ValidateOptionsResult Validate(string? name, EmailOptions options)
    {
        if (!options.IsConfigured)
        {
            logger.LogWarning(
                "Email sin configurar: no se enviaran correos de verificacion ni invitaciones. " +
                "Es la situacion esperada en desarrollo local.");
            return ValidateOptionsResult.Skip;
        }

        var failures = new List<string>();
        var prefix = EmailOptions.SectionName;

        if (options.Provider.Equals("Smtp", StringComparison.OrdinalIgnoreCase) &&
            string.IsNullOrWhiteSpace(options.Smtp.Host))
        {
            failures.Add($"{prefix}:Smtp:Host es obligatorio para el proveedor Smtp.");
        }

        if (options.Smtp.Port is < 1 or > 65535)
            failures.Add($"{prefix}:Smtp:Port debe estar entre 1 y 65535.");

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}