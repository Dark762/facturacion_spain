namespace Facturacion.Verifactu.Validation;

/// <summary>Error de validacion fiscal con su codigo estable.</summary>
public sealed record FiscalValidationError(string Code, string Message, string Field);

/// <summary>
/// Valida los datos fiscales antes de generar un registro.
/// Son reglas de negocio del proyecto: no pretenden ser la norma.
/// </summary>
public interface IVerifier
{
    bool IsAvailable { get; }

    IReadOnlyList<FiscalValidationError> Verify(FiscalData data);
}

public sealed record FiscalData(
    string IssuerNif,
    string InvoiceNumber,
    string? Series,
    DateOnly IssueDate,
    string InvoiceType,
    decimal TaxableBase,
    decimal TaxAmount,
    decimal Total,
    IReadOnlyList<FiscalLineData> Lines);

public sealed record FiscalLineData(
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    decimal VatRate,
    decimal LineTotal);

public sealed class Verifier : IVerifier
{
    public bool IsAvailable => true;

    public IReadOnlyList<FiscalValidationError> Verify(FiscalData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        var errors = new List<FiscalValidationError>();

        if (string.IsNullOrWhiteSpace(data.IssuerNif))
            errors.Add(new("ISSUER_NIF_REQUIRED", "El NIF del emisor es obligatorio.", nameof(data.IssuerNif)));

        if (string.IsNullOrWhiteSpace(data.InvoiceNumber))
            errors.Add(new("INVOICE_NUMBER_REQUIRED", "El numero de factura es obligatorio.", nameof(data.InvoiceNumber)));

        if (string.IsNullOrWhiteSpace(data.InvoiceType))
            errors.Add(new("INVOICE_TYPE_REQUIRED", "El tipo de factura es obligatorio.", nameof(data.InvoiceType)));

        if (data.IssueDate == default)
            errors.Add(new("ISSUE_DATE_REQUIRED", "La fecha de emision es obligatoria.", nameof(data.IssueDate)));

        if (data.IssueDate > DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1))
            errors.Add(new("ISSUE_DATE_IN_FUTURE", "La fecha de emision no puede estar en el futuro.", nameof(data.IssueDate)));

        if (data.Lines.Count == 0)
            errors.Add(new("INVOICE_HAS_NO_LINES", "La factura debe tener al menos una linea.", nameof(data.Lines)));

        if (data.Total <= 0m)
            errors.Add(new("TOTAL_NOT_POSITIVE", "El total debe ser mayor que cero.", nameof(data.Total)));

        if (data.TaxAmount < 0m)
            errors.Add(new("TAX_NEGATIVE", "El importe de impuesto no puede ser negativo.", nameof(data.TaxAmount)));

        // Redondeo a dos decimales: se admiten las diferencias de precision
        // binaria que produce decimal, pero no un descuadre real.
        if (data.Lines.Count > 0)
        {
            var lineSum = data.Lines.Sum(l => decimal.Round(l.LineTotal, 2, MidpointRounding.AwayFromZero));
            var total = decimal.Round(data.Total, 2, MidpointRounding.AwayFromZero);
            if (Math.Abs(lineSum - total) > 0.01m)
                errors.Add(new("LINES_TOTAL_MISMATCH",
                    $"La suma de las lineas ({lineSum:0.00}) no cuadra con el total ({total:0.00}).",
                    nameof(data.Total)));
        }

        foreach (var line in data.Lines)
        {
            if (string.IsNullOrWhiteSpace(line.Description))
                errors.Add(new("LINE_DESCRIPTION_REQUIRED", "Toda linea necesita descripcion.", nameof(line.Description)));
            if (line.Quantity <= 0m)
                errors.Add(new("LINE_QUANTITY_NOT_POSITIVE", "La cantidad de una linea debe ser mayor que cero.", nameof(line.Quantity)));
            if (line.VatRate is < 0m or > 100m)
                errors.Add(new("LINE_VAT_RATE_OUT_OF_RANGE", "El tipo impositivo debe estar entre 0 y 100.", nameof(line.VatRate)));
        }

        return errors;
    }
}