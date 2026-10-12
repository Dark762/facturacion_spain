using Facturacion.Verifactu;
using Facturacion.Verifactu.Canonicalization;
using Facturacion.Verifactu.Fingerprints;
using Xunit;

namespace Facturacion.Integration.Test;

/// <summary>
/// Conformidad de la huella con la especificacion oficial de la AEAT.
///
/// Documento: "Detalle de las especificaciones tecnicas para generacion de la
/// huella o hash de los registros de facturacion", version 0.1.2, de 27/08/2024.
///
/// Estos vectores son los que publica la Agencia Tributaria en los apartados
/// 6.1, 6.2 y 6.3 del documento. NO son ejemplos propios: reproducirlos es la
/// unica prueba valida de que la implementacion coincide con la norma.
/// </summary>
public sealed class FingerprintOfficialVectorsTests
{
    private const string Hash1 = "3C464DAF61ACB827C65FDA19F352A4E3BDC2C640E9E9FC4CC058073F38F12F60";
    private const string Hash2 = "F7B94CFD8924EDFF273501B01EE5153E4CE8F259766F88CF6ACB8935802A2B97";
    private const string Hash3 = "177547C0D57AC74748561D054A9CEC14B4C4EA23D1BEFD6F2E69E3A388F90C68";

    private static (AeatRecordCanonicalizer canonical, Sha256FingerprintCalculator calc) Build()
    {
        var canonical = new AeatRecordCanonicalizer();
        return (canonical, new Sha256FingerprintCalculator(canonical));
    }

    // ---------- Vectores oficiales ----------

    /// <summary>Apartado 6.1: primer registro, sin huella anterior.</summary>
    [Fact]
    public void Caso1_Primer_registro_de_alta()
    {
        var (_, calc) = Build();

        var request = new CanonicalizationRequest(
            FiscalRecordKind.Alta,
            IssuerNif: "89890001K",
            InvoiceNumber: "12345678/G33",
            IssueDate: "01-01-2024",
            InvoiceType: "F1",
            TaxTotal: "12.35",
            TotalAmount: "123.45",
            PreviousHash: null,
            GeneratedAt: "2024-01-01T19:20:30+01:00");

        var result = calc.Compute(request);

        Assert.Equal(Hash1, result.Hash);
        Assert.False(result.Chained);
    }

    /// <summary>Apartado 6.2: alta con registro anterior, encadenada.</summary>
    [Fact]
    public void Caso2_Alta_encadenada_con_registro_anterior()
    {
        var (_, calc) = Build();

        var request = new CanonicalizationRequest(
            FiscalRecordKind.Alta,
            IssuerNif: "89890001K",
            InvoiceNumber: "12345679/G34",
            IssueDate: "01-01-2024",
            InvoiceType: "F1",
            TaxTotal: "12.35",
            TotalAmount: "123.45",
            PreviousHash: Hash1,
            GeneratedAt: "2024-01-01T19:20:35+01:00");

        var result = calc.Compute(request);

        Assert.Equal(Hash2, result.Hash);
        Assert.True(result.Chained);
    }

    /// <summary>Apartado 6.3: registro de anulacion encadenado.</summary>
    [Fact]
    public void Caso3_Anulacion_encadenada()
    {
        var (_, calc) = Build();

        var request = new CanonicalizationRequest(
            FiscalRecordKind.Anulacion,
            IssuerNif: "89890001K",
            InvoiceNumber: "12345679/G34",
            IssueDate: "01-01-2024",
            InvoiceType: null,
            TaxTotal: null,
            TotalAmount: null,
            PreviousHash: Hash2,
            GeneratedAt: "2024-01-01T19:20:40+01:00");

        var result = calc.Compute(request);

        Assert.Equal(Hash3, result.Hash);
    }

    // ---------- La cadena canonica exacta ----------

    [Fact]
    public void La_cadena_canonica_del_caso1_es_la_publicada_por_la_AEAT()
    {
        var (canonical, _) = Build();

        var request = new CanonicalizationRequest(
            FiscalRecordKind.Alta, "89890001K", "12345678/G33", "01-01-2024",
            "F1", "12.35", "123.45", null, "2024-01-01T19:20:30+01:00");

        Assert.Equal(
            "IDEmisorFactura=89890001K&NumSerieFactura=12345678/G33" +
            "&FechaExpedicionFactura=01-01-2024&TipoFactura=F1" +
            "&CuotaTotal=12.35&ImporteTotal=123.45&Huella=" +
            "&FechaHoraHusoGenRegistro=2024-01-01T19:20:30+01:00",
            canonical.BuildCanonicalString(request));
    }

    [Fact]
    public void Un_primer_registro_conserva_el_par_Huella_vacio()
    {
        var (canonical, _) = Build();

        var s = canonical.BuildCanonicalString(new CanonicalizationRequest(
            FiscalRecordKind.Alta, "X", "1", "01-01-2024", "F1", "1", "1", null, "x"));

        Assert.Contains("&Huella=&", s);
    }

    // ---------- Reglas de normalizacion ----------

    [Fact]
    public void Los_valores_se_recortan_de_espacios()
    {
        var (canonical, _) = Build();

        var s = canonical.BuildCanonicalString(new CanonicalizationRequest(
            FiscalRecordKind.Alta, "  89890001K  ", "  12345678/G33  ",
            "01-01-2024", "F1", "12.35", "123.45", null, "2024-01-01T19:20:30+01:00"));

        Assert.Contains("IDEmisorFactura=89890001K&", s);
        Assert.Contains("NumSerieFactura=12345678/G33&", s);
    }

    [Fact]
    public void Los_ceros_a_la_derecha_se_conservan_tal_cual_llegan()
    {
        // La norma dice que 123.1 y 123.10 son ambos validos: la huella depende
        // de lo que diga el XML. No se puede "normalizar" por nuestra cuenta.
        var (canonical, _) = Build();

        var a = canonical.BuildCanonicalString(Make("123.45"));
        var b = canonical.BuildCanonicalString(Make("123.4500"));

        Assert.Contains("ImporteTotal=123.45&", a);
        Assert.Contains("ImporteTotal=123.4500&", b);
    }

    private static CanonicalizationRequest Make(string total) => new(
        FiscalRecordKind.Alta, "89890001K", "1", "01-01-2024", "F1", "12.35", total, null, "x");

    // ---------- Formato de salida ----------

    [Fact]
    public void La_huella_va_en_mayusculas_y_mide_64_caracteres()
    {
        var (_, calc) = Build();

        var result = calc.Compute(new CanonicalizationRequest(
            FiscalRecordKind.Alta, "89890001K", "12345678/G33", "01-01-2024",
            "F1", "12.35", "123.45", null, "2024-01-01T19:20:30+01:00"));

        Assert.Equal(64, result.Hash.Length);
        Assert.Equal(result.Hash.ToUpperInvariant(), result.Hash);
        Assert.Equal("SHA256", result.Algorithm);
    }

    [Fact]
    public void Una_huella_con_forma_no_oficial_se_rechaza()
    {
        Assert.True(Sha256FingerprintCalculator.HasOfficialShape(Hash1));
        Assert.False(Sha256FingerprintCalculator.HasOfficialShape(Hash1.ToLowerInvariant()));
        Assert.False(Sha256FingerprintCalculator.HasOfficialShape("corto"));
        Assert.False(Sha256FingerprintCalculator.HasOfficialShape(null));
    }

    [Fact]
    public void El_componente_queda_verificado()
    {
        Assert.True(new AeatRecordCanonicalizer().IsAvailable);
        Assert.True(Build().calc.IsAvailable);
        Assert.Equal("SHA256", FiscalSpecification.FingerprintAlgorithm);
        Assert.Equal("0.1.2", FiscalSpecification.HashSpecificationVersion);
    }
}