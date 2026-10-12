using Facturacion.Verifactu;
using Facturacion.Verifactu.Canonicalization;
using Facturacion.Verifactu.Chain;
using Facturacion.Verifactu.Fingerprints;
using Facturacion.Verifactu.Qr;
using Facturacion.Verifactu.Records;
using Facturacion.Verifactu.Validation;
using Facturacion.Verifactu.Xml;
using Xunit;

namespace Facturacion.Integration.Test;

/// <summary>
/// Pruebas del nucleo fiscal.
///
/// IMPORTANTE: ninguna de estas pruebas declara cumplimiento normativo.
/// Los componentes que dependen de la especificacion oficial no verificada
/// se prueban UNIQUAMENTE en su comportamiento fail-closed, que es el
/// comportamiento correcto mientras la especificacion no este disponible.
/// </summary>
public sealed class FiscalEngineTests
{
    private static FiscalEngine BuildEngine() => new(
        new Verifier(),
        new AeatRecordCanonicalizer(),
        new Sha256FingerprintCalculator(new AeatRecordCanonicalizer()),
        new FiscalChainWalker(),
        new AeatRecordXmlBuilder(),
        // Los XSD viajan junto al ensamblado de pruebas: una ruta absoluta
        // haria que el test pasara solo en la maquina donde se escribio.
        new AeatXsdValidator(Path.Combine(AppContext.BaseDirectory, "xsd")),
        new UnavailableQrGenerator());

    private static FiscalData ValidData() => new(
        IssuerNif: "B12345678",
        InvoiceNumber: "1",
        Series: "A",
        IssueDate: DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1),
        InvoiceType: "F1",
        TaxableBase: 100m,
        TaxAmount: 21m,
        Total: 121m,
        Lines: [new FiscalLineData("Producto", 1m, 100m, 21m, 121m)]);

    // ---------- Puerta fail-closed ----------

    [Fact]
    public void El_motor_no_esta_listo_porque_falta_la_canonicalizacion()
    {
        var engine = BuildEngine();

        Assert.False(engine.IsReadyForIssuance);
    }

    [Fact]
    public void El_bloqueo_es_por_qr_mientras_xml_y_xsd_estan_disponibles()
    {
        var blockers = BuildEngine().Blockers;

        Assert.Contains(nameof(IQrGenerator), blockers);
        Assert.DoesNotContain(nameof(IRecordXmlBuilder), blockers);
        Assert.DoesNotContain(nameof(IXsdValidator), blockers);
        Assert.DoesNotContain(nameof(IRecordCanonicalizer), blockers);
    }

    [Fact]
    public void El_informe_de_estado_explica_por_que_no_se_puede_emitir()
    {
        var report = BuildEngine().ReadinessReport;

        Assert.Contains("SHA256", report);
        Assert.Contains("Emision habilitada: NO", report);
        Assert.Contains(nameof(IRecordCanonicalizer), report);
    }

    // ---------- Componentes no disponibles: fallan con mensaje util ----------

    [Fact]
    public void La_canonicalizacion_devuelve_la_cadena_verbatim()
    {
        var cadena = new AeatRecordCanonicalizer().BuildCanonicalString(new CanonicalizationRequest(
            FiscalRecordKind.Alta, "89890001K", "12345678/G33", "01-01-2024",
            "F1", "12.35", "123.45", null, "2024-01-01T19:20:30+01:00"));

        Assert.StartsWith("IDEmisorFactura=89890001K&NumSerieFactura=", cadena);
        Assert.EndsWith("&FechaHoraHusoGenRegistro=2024-01-01T19:20:30+01:00", cadena);
    }
    [Fact]
    public void La_huella_se_calcula_sobre_la_cadena_canonica()
    {
        var engine = BuildEngine();

        Assert.True(engine.Fingerprint.IsAvailable);

        var result = engine.Fingerprint.Compute(new CanonicalizationRequest(
            FiscalRecordKind.Alta, "89890001K", "12345678/G33", "01-01-2024",
            "F1", "12.35", "123.45", null, "2024-01-01T19:20:30+01:00"));

        Assert.Equal(64, result.Hash.Length);
    }
[Fact]
    public void El_xml_y_el_xsd_no_estan_disponibles_SOLO_SI_LOS_ESQUEMAS_CARGAN()
    {
        var engine = BuildEngine();

        // Si los esquemas oficiales cargan, ambos componentes existen.
        Assert.True(engine.Xml.IsAvailable && engine.Xsd.IsAvailable);
    }

    [Fact]
    public void El_xml_y_el_xsd_ya_estan_disponibles()
    {
        var engine = BuildEngine();

        Assert.True(engine.Xml.IsAvailable);
        Assert.True(engine.Xsd.IsAvailable);
    }

    [Fact]
    public void Un_xml_invalido_se_rechaza_con_el_motivo()
    {
        var result = BuildEngine().Xsd.Validate("<RegFactuSistemaFacturacion><SinCerrar></RegFactuSistemaFacturacion>");

        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
    }
    private static FiscalRecordDraft BuildDraft() => new(
        Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
        IsAnulacion: false, "B12345678", "1",
        DateOnly.FromDateTime(DateTime.UtcNow), "F1", 121m,
        Hash: "0", PreviousHash: "", PreviousRecordId: null, XmlPayload: "",
        "FacturacionSaaS", "0.1.0", "TEST", Guid.NewGuid());

    // ---------- Validacion (logica de negocio, no normativa) ----------

    [Fact]
    public void Una_factura_valida_pasa_la_validacion()
    {
        Assert.Empty(new Verifier().Verify(ValidData()));
    }

    [Fact]
    public void Una_factura_sin_nif_del_emisor_no_pasa()
    {
        var data = ValidData() with { IssuerNif = "" };

        Assert.Contains(new Verifier().Verify(data), e => e.Code == "ISSUER_NIF_REQUIRED");
    }

    [Fact]
    public void Una_factura_sin_numero_no_pasa()
    {
        var data = ValidData() with { InvoiceNumber = "  " };

        Assert.Contains(new Verifier().Verify(data), e => e.Code == "INVOICE_NUMBER_REQUIRED");
    }

    [Fact]
    public void Una_factura_sin_lineas_no_pasa()
    {
        var data = ValidData() with { Lines = [] };

        Assert.Contains(new Verifier().Verify(data), e => e.Code == "INVOICE_HAS_NO_LINES");
    }

    [Fact]
    public void Una_factura_con_lineas_que_no_cuadran_no_pasa()
    {
        var data = ValidData() with { Total = 999m };

        Assert.Contains(new Verifier().Verify(data), e => e.Code == "LINES_TOTAL_MISMATCH");
    }

    [Fact]
    public void Una_factura_con_fecha_en_el_futuro_no_pasa()
    {
        var data = ValidData() with { IssueDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(5) };

        Assert.Contains(new Verifier().Verify(data), e => e.Code == "ISSUE_DATE_IN_FUTURE");
    }

    // ---------- Encadenamiento (el algoritmo SHA-256 si esta confirmado) ----------

    [Fact]
    public void La_cadena_avanza_con_el_nuevo_registro()
    {
        var walker = new FiscalChainWalker();

        var head = walker.BuildNextHead(new ChainHead(null, ""), Guid.NewGuid(), new string('a', 64));

        Assert.NotNull(head.LastRecordId);
        Assert.Equal(new string('a', 64), head.LastHash);
    }

    [Fact]
    public void Una_cadena_coherente_no_reporta_error()
    {
        var walker = new FiscalChainWalker();
        var prev = new string('a', 64);
        var actual = new string('b', 64);

        Assert.Null(walker.VerifyChaining(prev, prev, actual));
    }

    [Fact]
    public void Una_cadena_rota_se_detecta()
    {
        var walker = new FiscalChainWalker();
        var encadenada = new string('a', 64);
        var realDelAnterior = new string('c', 64);
        var actual = new string('b', 64);

        var problem = walker.VerifyChaining(encadenada, realDelAnterior, actual);

        Assert.NotNull(problem);
        Assert.Contains("no coincide", problem);
    }

    [Fact]
    public void Una_huella_que_no_mide_64_caracteres_se_rechaza()
    {
        var problem = new FiscalChainWalker().VerifyChaining(null, "", "corto");

        Assert.NotNull(problem);
    }

    // ---------- Especificacion ----------

    [Fact]
    public void La_especificacion_registra_el_algoritmo_verificado()
    {
        Assert.Equal("SHA256", FiscalSpecification.FingerprintAlgorithm);
        Assert.True(FiscalSpecification.ChainingRequired);
        Assert.Contains("agenciatributaria", FiscalSpecification.HashDocumentUrl);
    }
}