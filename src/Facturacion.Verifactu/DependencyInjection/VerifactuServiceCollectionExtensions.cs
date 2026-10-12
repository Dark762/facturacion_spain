using Facturacion.Verifactu;
using Facturacion.Verifactu.Canonicalization;
using Facturacion.Verifactu.Chain;
using Facturacion.Verifactu.Fingerprints;
using Facturacion.Verifactu.Qr;
using Facturacion.Verifactu.Records;
using Facturacion.Verifactu.Validation;
using Facturacion.Verifactu.Xml;
using Microsoft.Extensions.DependencyInjection;

namespace Facturacion.Verifactu.DependencyInjection;

/// <summary>
/// Ubicacion de los XSD oficiales de la AEAT.
/// </summary>
public sealed class FiscalSchemaOptions
{
    /// <summary>Configuracion: Fiscal:SchemaDirectory.</summary>
    public const string SectionName = "Fiscal";

    /// <summary>Carpeta que contiene los XSD oficiales.</summary>
    public string SchemaDirectory { get; set; } = "docs/verifactu/xsd";
}

public static class VerifactuServiceCollectionExtensions
{
    /// <summary>
    /// Registra el motor fiscal. Las implementaciones no disponibles se
    /// registran igualmente para que DI resuelva y el fallo ocurra en la
    /// ejecucion con un mensaje util, no al construir el contenedor.
    /// </summary>
    public static IServiceCollection AddVerifactu(this IServiceCollection services)
    {
        services.AddSingleton<Verifier>();
        services.AddSingleton<IVerifier>(sp => sp.GetRequiredService<Verifier>());

        // Canonicalizacion implementada y verificada contra la AEAT v0.1.2.
        services.AddSingleton<IRecordCanonicalizer, AeatRecordCanonicalizer>();
        services.AddSingleton<IFingerprintCalculator, Sha256FingerprintCalculator>();

        services.AddSingleton<IFiscalChainWalker, FiscalChainWalker>();

        // Esquemas oficiales cargados desde disco. El constructor del validador
        // es lazy: si la carpeta no existe, falla al validar y no al arrancar.
        services.AddSingleton<FiscalSchemaOptions>();
        services.AddSingleton<IRecordXmlBuilder, AeatRecordXmlBuilder>();
        services.AddSingleton<IXsdValidator>(sp =>
            new AeatXsdValidator(sp.GetRequiredService<FiscalSchemaOptions>().SchemaDirectory));

        // El QR sigue sin implementar: el motor permanece bloqueado.
        services.AddSingleton<IQrGenerator, UnavailableQrGenerator>();

        services.AddSingleton<IRecordBuilder, AltaRecordBuilder>();
        services.AddSingleton<IRecordBuilder, AnulacionRecordBuilder>();

        services.AddSingleton<IFiscalEngine, FiscalEngine>();
        return services;
    }
}
