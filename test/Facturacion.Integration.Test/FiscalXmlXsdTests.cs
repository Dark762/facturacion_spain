using Facturacion.Verifactu.Xml;
using Xunit;

namespace Facturacion.Integration.Test;

/// <summary>
/// Conformidad del XML fiscal con los XSD oficiales de la AEAT.
///
/// A diferencia de la huella, el XML NO tiene vectores oficiales publicados:
/// la conformidad se demuestra validando contra los esquemas oficiales, no
/// contra un ejemplo concreto.
///
/// Esquemas: AEAT, "Esquemas de los servicios web".
/// </summary>
public sealed class FiscalXmlXsdTests
{
    private static string SchemaDir =>
        Path.Combine(AppContext.BaseDirectory, "xsd");

    private static AeatXsdValidator Validator() => new(SchemaDir);

    private static AeatRecordXmlBuilder Builder() => new();

    private static FiscalRecordXmlData Sample(bool isFirst = true, string issuerNif = "89890001K") => new()
    {
        IssuerNif = issuerNif,
        IssuerName = "EMPRESA DEMO",
        InvoiceNumber = "12345678/G33",
        IssueDate = new DateOnly(2024, 1, 1),
        InvoiceType = "F1",
        Description = "Venta de Consulting",
        IsFirstRecord = isFirst,
        PreviousHash = isFirst ? null : "3C464DAF61ACB827C65FDA19F352A4E3BDC2C640E9E9FC4CC058073F38F12F60",
        PreviousInvoiceNumber = isFirst ? null : "12345677/G32",
        SystemName = "FacturacionSaaS",
        SystemId = "TS",
        SystemVersion = "0.1.0",
        InstallationNumber = "INSTALACION-0001",
        ProducerName = "PRODUCTORA DEMO",
        ProducerNif = "B12345678",
        TaxLines = [new TaxBreakdownLine("01", 100m, 21m, 21m)],
        TaxTotal = 21m,
        Total = 121m,
        Hash = "3C464DAF61ACB827C65FDA19F352A4E3BDC2C640E9E9FC4CC058073F38F12F60",
        GeneratedAt = new DateTimeOffset(2024, 1, 1, 19, 20, 30, TimeSpan.FromHours(1))
    };

    // ---------- Positivo ----------

    [Fact]
    public void Un_registro_de_alta_valida_contra_el_esquema_oficial()
    {
        var record = Builder().BuildAltaXml(Sample());
        var xml = Builder().BuildSubmissionXml("EMPRESA DEMO", "89890001K", record);

        var result = Validator().Validate(xml);

        Assert.True(result.IsValid, result.Summary);
    }

    [Fact]
    public void Un_registro_de_alta_encadenado_tambien_valida()
    {
        var record = Builder().BuildAltaXml(Sample(isFirst: false));
        var xml = Builder().BuildSubmissionXml("EMPRESA DEMO", "89890001K", record);

        var result = Validator().Validate(xml);

        Assert.True(result.IsValid, result.Summary);
    }

    // ---------- Negativos ----------

    [Fact]
    public void Un_nif_con_formato_invalido_se_rechaza()
    {
        var record = Builder().BuildAltaXml(Sample(issuerNif: "NO-ES-UN-NIF"));
        var xml = Builder().BuildSubmissionXml("EMPRESA DEMO", "89890001K", record);

        Assert.False(Validator().Validate(xml).IsValid);
    }

    [Fact]
    public void Una_huella_de_mas_de_64_caracteres_se_rechaza()
    {
        var data = Sample() with { Hash = new string('A', 65) };
        var xml = Builder().BuildSubmissionXml("EMPRESA DEMO", "89890001K", Builder().BuildAltaXml(data));

        Assert.False(Validator().Validate(xml).IsValid);
    }

    [Fact]
    public void Un_id_de_sistema_de_mas_de_2_caracteres_se_rechaza()
    {
        var data = Sample() with { SystemId = "DEMASIADO" };
        var xml = Builder().BuildSubmissionXml("EMPRESA DEMO", "89890001K", Builder().BuildAltaXml(data));

        Assert.False(Validator().Validate(xml).IsValid);
    }

    [Fact]
    public void Un_tipo_de_huella_distinto_de_01_se_rechaza()
    {
        var data = Sample() with { };
        // TipoHuella solo admite "01" segun el esquema.
        var record = Builder().BuildAltaXml(data).Replace(">01<", ">99<");
        var xml = Builder().BuildSubmissionXml("EMPRESA DEMO", "89890001K", record);

        Assert.False(Validator().Validate(xml).IsValid);
    }

    [Fact]
    public void Una_version_distinta_de_1_0_se_rechaza()
    {
        var record = Builder().BuildAltaXml(Sample()).Replace(">1.0<", ">2.0<");
        var xml = Builder().BuildSubmissionXml("EMPRESA DEMO", "89890001K", record);

        Assert.False(Validator().Validate(xml).IsValid);
    }

    // ---------- CalificacionOperacion (S1/S2/N1/N2) ----------

    [Theory]
    [InlineData("S1")]   // sujeta y no exenta, sin inversion del sujeto pasivo
    [InlineData("S2")]   // sujeta y no exenta, con inversion del sujeto pasivo
    [InlineData("N1")]   // no sujeta (arts. 7, 14 LIVA)
    [InlineData("N2")]   // no sujeta por reglas de localizacion
    public void Las_cuatro_calificaciones_validan_contra_el_esquema(string calificacion)
    {
        var data = Sample() with
        {
            TaxLines = [new TaxBreakdownLine("01", 100m, 21m, 21m) { CalificacionOperacion = calificacion }]
        };

        var xml = Builder().BuildSubmissionXml("EMPRESA DEMO", "89890001K", Builder().BuildAltaXml(data));

        Assert.True(Validator().Validate(xml).IsValid);
    }

    [Fact]
    public void Con_inversion_del_sujeto_pasivo_los_importes_van_a_cero()
    {
        // Regla AEAT: en S2 no seVegeta %, tipo impositivo y cuota a cero.
        var data = Sample() with
        {
            TaxLines = [new TaxBreakdownLine("01", 100m, 21m, 21m) { CalificacionOperacion = "S2" }]
        };

        var xml = Builder().BuildAltaXml(data);

        Assert.Contains(">S2<", xml);
        Assert.Equal(2, CountOf(xml, ">0.00<"));   // TipoImpositivo y CuotaRepercutida
    }

    [Fact]
    public void Una_venta_con_iva_normal_usa_S1_por_defecto()
    {
        Assert.Contains(">S1<", Builder().BuildAltaXml(Sample()));
    }

    [Fact]
    public void Una_calificacion_fuera_del_conjunto_se_rechaza()
    {
        var data = Sample() with
        {
            TaxLines = [new TaxBreakdownLine("01", 100m, 21m, 21m) { CalificacionOperacion = "X9" }]
        };

        var xml = Builder().BuildSubmissionXml("EMPRESA DEMO", "89890001K", Builder().BuildAltaXml(data));

        Assert.False(Validator().Validate(xml).IsValid);
    }

    private static int CountOf(string haystack, string needle) =>
        haystack.Split(needle, StringSplitOptions.None).Length - 1;

    // ---------- Seguridad ----------

    [Fact]
    public void Un_documento_con_entidad_externa_no_se_procesa()
    {
        // Una DTD es la via clasica de XXE. El validador la prohibe.
        const string xml = """
            <?xml version="1.0"?>
            <!DOCTYPE foo [ <!ENTITY xxe SYSTEM "file:///etc/passwd"> ]>
            <RegFactuSistemaFacturacion xmlns="https://www2.agenciatributaria.gob.es/static_files/common/internet/dep/aplicaciones/es/aeat/tike/cont/ws/SuministroLR.xsd"/>
            """;

        Assert.False(Validator().Validate(xml).IsValid);
    }

    [Fact]
    public void El_validador_registra_la_version_del_esquema_aplicada()
    {
        Assert.Contains("0.1.2", Validator().SchemaReference);
        Assert.Contains("AEAT", Validator().SchemaReference);
    }

    [Fact]
    public void Un_registro_de_anulacion_valida_contra_el_esquema_oficial()
    {
        var record = Builder().BuildAnulacionXml(Sample());
        var xml = Builder().BuildSubmissionXml("EMPRESA DEMO", "89890001K", record);

        var result = Validator().Validate(xml);

        Assert.True(result.IsValid, result.Summary);
    }

    [Fact]
    public void Un_registro_de_anulacion_encadenado_tambien_valida()
    {
        var record = Builder().BuildAnulacionXml(Sample(isFirst: false));
        var xml = Builder().BuildSubmissionXml("EMPRESA DEMO", "89890001K", record);

        var result = Validator().Validate(xml);

        Assert.True(result.IsValid, result.Summary);
    }

    [Fact]
    public void Un_registro_con_namespace_invalido_se_rechaza()
    {
        var record = Builder().BuildAltaXml(Sample());
        var xml = Builder().BuildSubmissionXml("EMPRESA DEMO", "89890001K", record)
            .Replace("https://www2.agenciatributaria.gob.es/static_files/common/internet/dep/aplicaciones/es/aeat/tike/cont/ws/SuministroLR.xsd", "urn:namespace-falso");

        Assert.False(Validator().Validate(xml).IsValid);
    }

    [Fact]
    public void Un_validador_con_directorio_inexistente_falla_seguro()
    {
        var validator = new AeatXsdValidator(Path.Combine(AppContext.BaseDirectory, "no_existe_directorio_xsd"));
        Assert.False(validator.IsAvailable);

        var res = validator.Validate("<test/>");
        Assert.False(res.IsValid);
        Assert.Contains("no estan disponibles", res.Summary);
    }

    [Fact]
    public void El_xml_se_genera_en_utf8_sin_BOM()
    {
        var xml = Builder().BuildAltaXml(Sample());

        Assert.StartsWith("<?xml version=\"1.0\" encoding=\"utf-8\"?>", xml);

        // El BOM se comprueba sobre los BYTES, no sobre el texto decodificado:
        // UTF8Encoding(false) no lo emite y GetString nunca lo inventaria.
        var bytes = System.Text.Encoding.UTF8.GetBytes(xml);
        Assert.False(bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF,
            "El XML no debe llevar BOM: la AEAT lo rechaza.");
    }
}