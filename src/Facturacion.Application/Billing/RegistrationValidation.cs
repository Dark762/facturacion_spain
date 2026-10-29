using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Facturacion.Application.Billing;

public sealed class RegistrationValidationException : Exception
{
    public IReadOnlyDictionary<string, string[]> Errors { get; }
    public RegistrationValidationException(IDictionary<string, string[]> errors)
        : base("Comprueba los campos indicados.")
        => Errors = new Dictionary<string, string[]>(errors);
}

public static class RegistrationValidation
{
    public static IReadOnlyDictionary<string, string[]> Validate(RegisterSaasRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        void Required(string key, string? value, string name)
        {
            if (string.IsNullOrWhiteSpace(value))
                errors[key] = new[] { $"El campo {name} es obligatorio." };
        }

        Required(nameof(request.DisplayName), request.DisplayName, "nombre completo");
        Required(nameof(request.Email), request.Email, "correo electrónico");
        Required(nameof(request.Password), request.Password, "contraseña");
        if (!errors.ContainsKey(nameof(request.Email)) &&
            !new EmailAddressAttribute().IsValid(request.Email.Trim()))
            errors[nameof(request.Email)] = new[] { "Introduce un correo electrónico válido." };
        if (!errors.ContainsKey(nameof(request.Password)) && request.Password.Length < RegistrationRules.MinimumPasswordLength)
            errors[nameof(request.Password)] = new[] { "La contraseña debe tener al menos 12 caracteres." };
        return errors;
    }

    public static void ThrowIfInvalid(RegisterSaasRequest request)
    {
        var errors = Validate(request);
        if (errors.Count != 0)
            throw new RegistrationValidationException(errors.ToDictionary(x => x.Key, x => x.Value));
    }
    public static IReadOnlyDictionary<string, string[]> ValidateCompany(CompleteCompanyRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        void Required(string key, string? value, string name)
        {
            if (string.IsNullOrWhiteSpace(value))
                errors[key] = new[] { $"El campo {name} es obligatorio." };
        }
        Required(nameof(request.LegalName), request.LegalName, request.HolderType.Is(FiscalHolderType.Individual) ? "nombre y apellidos" : "razón social");
        if (!RegistrationValues.IsValidFiscalHolder(request.HolderType))
            errors[nameof(request.HolderType)] = new[] { "Selecciona persona física o persona jurídica." };
        Required(nameof(request.Nif), request.Nif, "NIF");
        Required(nameof(request.Address), request.Address, "dirección");
        Required(nameof(request.PostalCode), request.PostalCode, "código postal");
        Required(nameof(request.City), request.City, "ciudad");
        Required(nameof(request.Province), request.Province, "provincia");
        if (!errors.ContainsKey(nameof(request.PostalCode)) &&
            !Regex.IsMatch(request.PostalCode.Trim(), @"^\d{5}$"))
            errors[nameof(request.PostalCode)] = new[] { "El código postal debe contener 5 dígitos." };
        if (!errors.ContainsKey(nameof(request.Nif)))
        {
            var nif = request.Nif.Trim().ToUpperInvariant();
            var validFormat = request.HolderType == "Individual"
                ? Regex.IsMatch(nif, @"^(?:[0-9]{8}[A-Z]|[XYZ][0-9]{7}[A-Z])$")
                : Regex.IsMatch(nif, @"^[ABCDEFGHJNPQRSUVW][0-9]{7}[0-9A-J]$");
            if (!validFormat)
                errors[nameof(request.Nif)] = new[] { request.HolderType == "Individual"
                    ? "Introduce un NIF o NIE de persona física con formato válido."
                    : "Introduce un NIF de entidad con formato válido." };
        }
        return errors;
    }

    public static void ThrowIfInvalidCompany(CompleteCompanyRequest request)
    {
        var errors = ValidateCompany(request);
        if (errors.Count != 0)
            throw new RegistrationValidationException(errors.ToDictionary(x => x.Key, x => x.Value));
    }

}
