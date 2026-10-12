using System.Xml;
using System.Xml.Schema;
using Xunit;

namespace Facturacion.Integration.Test;

/// <summary>
/// Estado del soporte XSD.
///
/// HALLAZGO ABIERTO: los esquemas oficiales de la AEAT referencian el elemento
/// ds:Signature del namespace de firma XML de W3C
/// (http://www.w3.org/2000/09/xmldsig#) SIN declarar el &lt;import&gt; de ese
/// namespace. Por eso XmlSchemaSet.Compile() falla con:
///
///     The 'http://www.w3.org/2000/09/xmldsig#:Signature' element is not declared.
///
/// Habilitar XmlUrlResolver no lo resuelve: no hay import que seguir.
///
/// SOLUCION PENDIENTE: incorporar el esquema xmldsig-core-schema.xsd de W3C al
/// conjunto ANTES de compilar, o alterar el conjunto para ignorar ese elemento.
///
/// Hasta resolverlo, IRecordXmlBuilder e IXsdValidator siguen marcados como
/// Unavailable_MissingSpecification y la emision permanece bloqueada.
///
/// Estas pruebas fijan el comportamiento ACTUAL a proposito: cuando se corrija,
/// la de "el esquema compila" dejara de compilar y habra que reescribirla.
/// </summary>
public sealed class XsdToolchainTests
{
    private const string SuministroLrNs =
        "https://www2.agenciatributaria.gob.es/static_files/common/internet/dep/aplicaciones/es/aeat/tike/cont/ws/SuministroLR.xsd";

    private const string SuministroInformacionNs =
        "https://www2.agenciatributaria.gob.es/static_files/common/internet/dep/aplicaciones/es/aeat/tike/cont/ws/SuministroInformacion.xsd";

    private static string XsdDir => Path.Combine(AppContext.BaseDirectory, "xsd");

    private static XmlSchemaSet LoadSchemas(bool resolver = false)
    {
        Assert.True(Directory.Exists(XsdDir), $"No se encuentra el directorio de XSD: {XsdDir}");

        var schemas = new XmlSchemaSet
        {
            XmlResolver = resolver ? new XmlUrlResolver() : null
        };

        // Orden importante: primero el esquema de firma XML de W3C, que
        // SuministroInformacion.xsd importa por URL remota. Cargarlo aqui y de
        // forma explicita evita depender de la red en tiempo de prueba.
        var sig = Path.Combine(XsdDir, "xmldsig-core-schema.xsd");
        if (File.Exists(sig))
        {
            using var sigReader = XmlReader.Create(sig);
            schemas.Add("http://www.w3.org/2000/09/xmldsig#", sigReader);
        }

        foreach (var file in new[] { "SuministroInformacion.xsd", "SuministroLR.xsd" })
        {
            var path = Path.Combine(XsdDir, file);
            Assert.True(File.Exists(path), $"Falta el esquema {file}");
            using var reader = XmlReader.Create(path);
            schemas.Add(null, reader);
        }

        return schemas;
    }

    [Fact]
    public void Los_seis_esquemas_oficiales_estan_disponibles()
    {
        foreach (var file in new[]
                 {
                     "SuministroLR.xsd", "SuministroInformacion.xsd", "RespuestaSuministro.xsd",
                     "ConsultaLR.xsd", "RespuestaConsultaLR.xsd", "EventosSIF.xsd"
                 })
        {
            Assert.True(File.Exists(Path.Combine(XsdDir, file)), $"Falta {file}");
        }
    }

    [Fact]
    public void Los_esquemas_declaran_los_namespace_esperados()
    {
        var schemas = LoadSchemas();
        var namespaces = new List<string?>();

        foreach (XmlSchema schema in schemas.Schemas())
            namespaces.Add(schema.TargetNamespace);

        Assert.Contains(SuministroLrNs, namespaces);
        Assert.Contains(SuministroInformacionNs, namespaces);
    }

    [Fact]
    public void El_esquema_compila_con_la_firma_xml_incorporada()
    {
        var schemas = LoadSchemas();
        schemas.Compile();

        var namespaces = new List<string?>();
        foreach (XmlSchema schema in schemas.Schemas())
            namespaces.Add(schema.TargetNamespace);

        Assert.Contains("http://www.w3.org/2000/09/xmldsig#", namespaces);
    }}