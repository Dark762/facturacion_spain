using Facturacion.Configuration.Options;
using Microsoft.Extensions.Options;

namespace Facturacion.Configuration.Validation;

/// <summary>
/// Cola de emision. Obligatoria en el worker: un lote de 0 o una lease
/// insuficiente devolverian los mensajes a la cola en bucle.
/// </summary>
public sealed class OutboxOptionsValidator : IValidateOptions<OutboxOptions>
{
    public ValidateOptionsResult Validate(string? name, OutboxOptions options)
    {
        var failures = new List<string>();
        var prefix = OutboxOptions.SectionName;

        if (options.BatchSize is < 1 or > 500)
            failures.Add($"{prefix}:BatchSize debe estar entre 1 y 500.");

        if (options.MaxAttempts < 1)
            failures.Add($"{prefix}:MaxAttempts debe ser al menos 1.");

        if (options.PollInterval <= TimeSpan.Zero)
            failures.Add($"{prefix}:PollInterval debe ser positivo.");

        if (options.Lease <= TimeSpan.Zero)
            failures.Add($"{prefix}:Lease debe ser positivo.");

        if (options.BaseDelay < TimeSpan.Zero)
            failures.Add($"{prefix}:BaseDelay no puede ser negativo.");

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}