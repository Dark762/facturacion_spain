namespace Facturacion.Api.Validation;

/// <summary>Validación de identificadores españoles; no presupone formatos de otros países.</summary>
public static class SpanishTaxIdValidator
{
    private const string NifLetters = "TRWAGMYFPDXBNJZSQVHLCKE";
    private const string CifLetters = "JABCDEFGHI";

    public static bool IsValid(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;
        var id = value.Trim().ToUpperInvariant();
        if (id.Length != 9 || !id.All(char.IsAsciiLetterOrDigit)) return false;

        if (id[..8].All(char.IsAsciiDigit) && char.IsAsciiLetter(id[8]))
            return NifLetters[int.Parse(id[..8], System.Globalization.CultureInfo.InvariantCulture) % 23] == id[8];

        if ("XYZ".Contains(id[0]) && id.Substring(1, 7).All(char.IsAsciiDigit) && char.IsAsciiLetter(id[8]))
        {
            var number = (id[0] - 'X').ToString(System.Globalization.CultureInfo.InvariantCulture) + id.Substring(1, 7);
            return NifLetters[int.Parse(number, System.Globalization.CultureInfo.InvariantCulture) % 23] == id[8];
        }

        if (!"ABCDEFGHJNPQRSUVW".Contains(id[0]) || !id.Substring(1, 7).All(char.IsAsciiDigit))
            return false;

        var sum = 0;
        for (var i = 1; i <= 7; i++)
        {
            var digit = id[i] - '0';
            if (i % 2 == 1)
            {
                var doubled = digit * 2;
                sum += doubled / 10 + doubled % 10;
            }
            else sum += digit;
        }
        var check = (10 - sum % 10) % 10;
        var expectedDigit = (char)('0' + check);
        var expectedLetter = CifLetters[check];
        return id[0] switch
        {
            'A' or 'B' or 'E' or 'H' => id[8] == expectedDigit,
            'N' or 'P' or 'Q' or 'S' or 'W' => id[8] == expectedLetter,
            _ => id[8] == expectedDigit || id[8] == expectedLetter
        };
    }
}
