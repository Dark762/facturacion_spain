using System.Globalization;

namespace Facturacion.Application.Services;

/// <summary>
/// Identificacion del sistema informatico de facturacion.
///
/// Los campos NO son equivalentes entre si. El esquema oficial de la AEAT
/// (SuministroInformacion.xsd, namespace .../ws/SuministroInformacion.xsd)
/// los separa en dos bloques distintos:
///
/// SistemaInformatico/NombreRazon + NIF|IDOtro
///     Identificacion del FABRICANTE O PRODUCTOR del software. No es el
///     emisor de la factura: es quien desarrolla el programa.
///
/// SistemaInformatico/NombreSistemaInformatico, IdSistemaInformatico,
///     Version, NumeroInstalacion
///     Identificacion del SISTEMA INFORMATICO concreto y de esta
///     INSTALACION.
///
/// Se mantiene la distincion porque mezclarlas haria que un registro
/// identificara a la empresa equivocada.
///
/// Limites tomados del esquema, no inventados:
///   NombreRazon            maxLength 120
///   NombreSistemaInformatico maxLength 30
///   IdSistemaInformatico   maxLength 2
///   Version                maxLength 50
///   NumeroInstalacion      maxLength 100
///   IDVersion              VersionType, unica enumeracion valida "1.0"
///   TipoHuella             TipoHuellaType, unica enumeracion valida "01"
/// </summary>
public sealed record FiscalSystemIdentification
{
    public const string SectionName = "Verifactu:SystemIdentification";

    /// <summary>Valor unico admitido por VersionType en el esquema.</summary>
    public const string AllowedIdVersion = "1.0";

    /// <summary>Valor unico admitido por TipoHuellaType (SHA-256).</summary>
    public const string AllowedHashType = "01";

    // --- Fabricante o productor del software ---

    public string ProducerName { get; init; } = "";

    /// <summary>NIF del productor. Alternativa excluyente a ProducerOtherId.</summary>
    public string? ProducerNif { get; init; }

    /// <summary>Identificador alternativo cuando el productor no tiene NIF.</summary>
    public string? ProducerOtherId { get; init; }

    public string? ProducerCountryCode { get; init; }

    public string? ProducerIdType { get; init; }

    // --- Sistema informatico ---

    public string SystemName { get; init; } = "";

    /// <summary>Codigo de 2 caracteres como maximo, segun el esquema.</summary>
    public string SystemId { get; init; } = "";

    public string SystemVersion { get; init; } = "";

    /// <summary>Identificador de esta instalacion concreta.</summary>
    public string InstallationNumber { get; init; } = "";

    // --- Valores de esquema ---

    public string IdVersion { get; init; } = AllowedIdVersion;

    public string HashType { get; init; } = AllowedHashType;

    // --- Indicadores SiNoType (S / N) ---

    public string CanOnlyBeUsedForVerifactu { get; init; } = "N";

    public string CanBeUsedForOtherRegimes { get; init; } = "N";

    public string HasMultipleInstallations { get; init; } = "N";

    public bool IsValid(out IReadOnlyList<string> errors)
    {
        var problems = new List<string>();

        Check(problems, ProducerName, nameof(ProducerName), 120);
        Check(problems, SystemName, nameof(SystemName), 30);
        Check(problems, SystemId, nameof(SystemId), 2);
        Check(problems, SystemVersion, nameof(SystemVersion), 50);
        Check(problems, InstallationNumber, nameof(InstallationNumber), 100);

        if (string.IsNullOrWhiteSpace(ProducerNif) && string.IsNullOrWhiteSpace(ProducerOtherId))
            problems.Add(
                $"{nameof(ProducerNif)} o {nameof(ProducerOtherId)}: el esquema exige uno de " +
                "los dos, de forma excluyente.");

        if (!string.IsNullOrWhiteSpace(ProducerNif) && !string.IsNullOrWhiteSpace(ProducerOtherId))
            problems.Add("El esquema exige NIF o IDOtro, pero nunca los dos a la vez.");

        if (IdVersion != AllowedIdVersion)
            problems.Add($"{nameof(IdVersion)} solo admite '{AllowedIdVersion}' (VersionType).");

        if (HashType != AllowedHashType)
            problems.Add($"{nameof(HashType)} solo admite '{AllowedHashType}' (TipoHuellaType).");

        CheckSiNo(problems, CanOnlyBeUsedForVerifactu, nameof(CanOnlyBeUsedForVerifactu));
        CheckSiNo(problems, CanBeUsedForOtherRegimes, nameof(CanBeUsedForOtherRegimes));
        CheckSiNo(problems, HasMultipleInstallations, nameof(HasMultipleInstallations));

        errors = problems;
        return problems.Count == 0;
    }

    private static void Check(List<string> problems, string? value, string name, int max)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            problems.Add($"{name} es obligatorio.");
            return;
        }

        if (IsPlaceholder(value))
            problems.Add($"{name} sigue con el valor marcador '{value}': debe configurarse.");

        if (value.Trim().Length > max)
            problems.Add($"{name} excede el maximo de {max} caracteres del esquema.");
    }

    private static void CheckSiNo(List<string> problems, string value, string name)
    {
        if (value != "S" && value != "N")
            problems.Add($"{name} solo admite 'S' o 'N' (SiNoType). Valor actual: '{value}'.");
    }

    private static bool IsPlaceholder(string value) =>
        value.Contains("PENDIENTE", StringComparison.OrdinalIgnoreCase)
        || value.Contains("CAMBIAR", StringComparison.OrdinalIgnoreCase)
        || value.Contains("TODO", StringComparison.OrdinalIgnoreCase)
        || value.Contains("REEMPLAZAR", StringComparison.OrdinalIgnoreCase)
        || value.Contains("CHANGE-ME", StringComparison.OrdinalIgnoreCase);
}

/// <summary>
/// Identificacion vigente del sistema.
///
/// Se construye UNA vez desde la configuracion. Si falta algun valor
/// obligatorio, <see cref="IsComplete"/> es false y la emision sigue
/// bloqueada: es preferible no emitir a emitir con una identificacion
/// ficticia en un documento que va a Hacienda.
/// </summary>
public static class FiscalEngineSoftware
{
    private static FiscalSystemIdentification _current = FromEnvironment();

    public static FiscalSystemIdentification Current => _current;

    public static bool IsComplete => _current.IsValid(out _);

    /// <summary>Motivos por los que la identificacion no es valida.</summary>
    public static string Describe()
    {
        if (_current.IsValid(out var errors))
            return "Identificacion del sistema valida.";

        return "Identificacion del sistema NO valida:" + Environment.NewLine +
               string.Join(Environment.NewLine, errors.Select(e => "  - " + e));
    }

    /// <summary>
    /// Sustituye la identificacion. Pensado para pruebas y para el arranque
    /// desde configuracion; si lo que se pasa no es valido, NO se acepta y se
    /// conserva la anterior, de modo que un valor invalido no pueda degradar
    /// silenciosamente el sistema a un estado "medio configurado".
    /// </summary>
    public static bool TrySet(FiscalSystemIdentification identification)
    {
        ArgumentNullException.ThrowIfNull(identification);

        if (!identification.IsValid(out _))
            return false;

        _current = identification;
        return true;
    }

    /// <summary>
    /// Lee la configuracion del entorno. Sin configuracion, TODOS los campos
    /// quedan vacios y el sistema se bloquea. No se inventa ningun valor por
    /// defecto: un nombre de software inventado acabaria en el registro fiscal.
    /// </summary>
    private static FiscalSystemIdentification FromEnvironment() => new()
    {
        ProducerName = Environment.GetEnvironmentVariable("VERIFACTU_PRODUCER_NAME") ?? "",
        ProducerNif = Environment.GetEnvironmentVariable("VERIFACTU_PRODUCER_NIF"),
        ProducerOtherId = Environment.GetEnvironmentVariable("VERIFACTU_PRODUCER_OTHER_ID"),
        ProducerCountryCode = Environment.GetEnvironmentVariable("VERIFACTU_PRODUCER_COUNTRY"),
        ProducerIdType = Environment.GetEnvironmentVariable("VERIFACTU_PRODUCER_ID_TYPE"),
        SystemName = Environment.GetEnvironmentVariable("VERIFACTU_SYSTEM_NAME") ?? "",
        SystemId = Environment.GetEnvironmentVariable("VERIFACTU_SYSTEM_ID") ?? "",
        SystemVersion = Environment.GetEnvironmentVariable("VERIFACTU_SYSTEM_VERSION") ?? "",
        InstallationNumber = Environment.GetEnvironmentVariable("VERIFACTU_INSTALLATION") ?? "",
        IdVersion = Environment.GetEnvironmentVariable("VERIFACTU_ID_VERSION") ?? FiscalSystemIdentification.AllowedIdVersion,
        HashType = Environment.GetEnvironmentVariable("VERIFACTU_HASH_TYPE") ?? FiscalSystemIdentification.AllowedHashType
    };
}