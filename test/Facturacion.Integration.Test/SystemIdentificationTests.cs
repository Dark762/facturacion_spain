using Facturacion.Application.Services;
using Xunit;

namespace Facturacion.Integration.Test;

/// <summary>
/// Validacion de la identificacion del sistema informatico.
///
/// Reglas tomadas del esquema oficial de la AEAT
/// (SuministroInformacion.xsd), no inventadas:
///   NombreSistemaInformatico maxLength 30
///   IdSistemaInformatico     maxLength 2
///   Version                  maxLength 50
///   NumeroInstalacion        maxLength 100
///   IDVersion                VersionType, unico valor "1.0"
///   TipoHuella               TipoHuellaType, unico valor "01"
///   Indicadores              SiNoType, "S" o "N"
///   Fabricante               NombreRazon + (NIF XOR IDOtro)
/// </summary>
public sealed class SystemIdentificationTests
{
    private static FiscalSystemIdentification Valid() => TestSystemIdentificationHelper.Valid();

    [Fact]
    public void Una_identificacion_correcta_es_valida()
    {
        Assert.True(Valid().IsValid(out var errors), string.Join("; ", errors));
    }

    [Fact]
    public void Un_nombre_de_sistema_mas_largo_de_30_se_rechaza()
    {
        var id = Valid() with { SystemName = new string('A', 31) };

        Assert.False(id.IsValid(out var errors));
        Assert.Contains(errors, e => e.Contains(nameof(FiscalSystemIdentification.SystemName)));
    }

    [Fact]
    public void Un_id_de_sistema_mas_largo_de_2_se_rechaza()
    {
        // El esquema limita IdSistemaInformatico a 2 caracteres.
        var id = Valid() with { SystemId = "ABC" };

        Assert.False(id.IsValid(out var errors));
        Assert.Contains(errors, e => e.Contains(nameof(FiscalSystemIdentification.SystemId)));
    }

    [Fact]
    public void El_fabricante_exige_nif_o_idotro_pero_no_ambos()
    {
        var sinNada = Valid() with { ProducerNif = null, ProducerOtherId = null };
        Assert.False(sinNada.IsValid(out var e1));
        Assert.Contains(e1, e => e.Contains("excluyente"));

        var ambos = Valid() with { ProducerOtherId = "X1234567X" };
        Assert.False(ambos.IsValid(out var e2));
        Assert.Contains(e2, e => e.Contains("nunca los dos"));

        var conOtro = Valid() with { ProducerNif = null, ProducerOtherId = "X1234567X" };
        Assert.True(conOtro.IsValid(out var e3), string.Join("; ", e3));
    }

    [Fact]
    public void IdVersion_solo_admite_1_0()
    {
        var id = Valid() with { IdVersion = "2.0" };

        Assert.False(id.IsValid(out var errors));
        Assert.Contains(errors, e => e.Contains(nameof(FiscalSystemIdentification.IdVersion)));
    }

    [Fact]
    public void Los_indicadores_solo_admiten_S_o_N()
    {
        var id = Valid() with { CanOnlyBeUsedForVerifactu = "SI" };

        Assert.False(id.IsValid(out var errors));
        Assert.Contains(errors, e => e.Contains(nameof(FiscalSystemIdentification.CanOnlyBeUsedForVerifactu)));
    }

    [Theory]
    [InlineData("PENDIENTE-DE-CONFIGURAR")]
    [InlineData("pendiente de configurar")]
    [InlineData("CAMBIAR-ME")]
    [InlineData("REEMPLAZAR")]
    public void Un_valor_marcador_se_rechaza(string marcador)
    {
        var id = Valid() with { InstallationNumber = marcador };

        Assert.False(id.IsValid(out var errors));
        Assert.Contains(errors, e => e.Contains("valor marcador"));
    }

    [Fact]
    public void Un_campo_obligatorio_vacio_se_rechaza()
    {
        var id = Valid() with { SystemName = "" };

        Assert.False(id.IsValid(out var errors));
        Assert.Contains(errors, e => e.Contains("obligatorio"));
    }

    [Fact]
    public void Una_identificacion_invalida_no_puede_sustituir_a_la_vigente()
    {
        var original = FiscalEngineSoftware.Current;

        var aceptada = FiscalEngineSoftware.TrySet(Valid() with { SystemId = "DEMASIADO-LARGO" });

        Assert.False(aceptada);
        Assert.Equal(original, FiscalEngineSoftware.Current);
    }

    [Fact]
    public void Una_identificacion_valida_si_se_acepta()
    {
        var valida = Valid() with { InstallationNumber = "INSTALACION-XYZ" };

        Assert.True(FiscalEngineSoftware.TrySet(valida));
        Assert.Equal("INSTALACION-XYZ", FiscalEngineSoftware.Current.InstallationNumber);
        Assert.True(FiscalEngineSoftware.IsComplete);
    }

    [Fact]
    public void Proveedor_inyectado_por_di_aisla_la_configuracion_sin_mutar_estado_global()
    {
        var globalOriginal = FiscalEngineSoftware.Current;

        var customIdentification = Valid() with
        {
            InstallationNumber = "INSTALACION-AISLADA-DI",
            SystemName = "SistemaCustomDI"
        };

        var provider = new FiscalSystemProvider(customIdentification);

        Assert.True(provider.IsComplete);
        Assert.Equal("INSTALACION-AISLADA-DI", provider.Current.InstallationNumber);
        Assert.Equal("SistemaCustomDI", provider.Current.SystemName);

        // El estado global permanece inalterado
        Assert.Equal(globalOriginal, FiscalEngineSoftware.Current);
    }
}

/// <summary>Acceso al helper sin exponerlo como parte del dominio.</summary>
internal static class TestSystemIdentificationHelper
{
    public static FiscalSystemIdentification Valid() =>
        Facturacion.Integration.Test.Infrastructure.TestSystemIdentification.Valid();
}