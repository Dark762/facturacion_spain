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

public static class VerifactuServiceCollectionExtensions
{
    /// <summary>
    /// Registra el motor fiscal. Las implementaciones no disponibles se
    /// registran igualmente para que DI resuelva y el fallo ocurra en la
    /// ejecucion con un mensaje util, no al construir el contenedor.
    /// </summary>
    /// <param name="schemaDirectory">
    /// Carpeta con los XSD oficiales de la AEAT. Se recibe como cadena y no
    /// como <c>IOptions</c> para que este proyecto siga sin depender de la
    /// configuracion: quien arranca decide, este proyecto solo ejecuta.
    /// </param>
    public static IServiceCollection AddVerifactu(this IServiceCollection services, string schemaDirectory)
    {
        services.AddSingleton<Verifier>();
        services.AddSingleton<IVerifier>(sp => sp.GetRequiredService<Verifier>());

        // Canonicalizacion implementada y verificada contra la AEAT v0.1.2.
        services.AddSingleton<IRecordCanonicalizer, AeatRecordCanonicalizer>();
        services.AddSingleton<IFingerprintCalculator, Sha256FingerprintCalculator>();

        services.AddSingleton<IFiscalChainWalker, FiscalChainWalker>();

        // Esquemas oficiales cargados desde disco. El constructor del validador
        // es lazy: si la carpeta no existe, falla al validar y no al arrancar.
        services.AddSingleton<IRecordXmlBuilder, AeatRecordXmlBuilder>();
        services.AddSingleton<IXsdValidator>(_ => new AeatXsdValidator(schemaDirectory));

        // El QR sigue sin implementar: el motor permanece bloqueado.
        services.AddSingleton<IQrGenerator, UnavailableQrGenerator>();

        services.AddSingleton<IRecordBuilder, AltaRecordBuilder>();
        services.AddSingleton<IRecordBuilder, AnulacionRecordBuilder>();

        services.AddSingleton<IFiscalEngine, FiscalEngine>();
        return services;
    }
}
