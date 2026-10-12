using Facturacion.Application.Common.Results;
using Facturacion.Application.DTOs.Invoices;
using Facturacion.Application.Interfaces;
using Facturacion.Integration.Test.Infrastructure;
using Facturacion.Verifactu.Xml;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Facturacion.Integration.Test;

/// <summary>
/// El XML fiscal se genera y se valida en la propia emision.
///
/// Antes el registro se guardaba con XmlPayload vacio. El worker lo detectaba,
/// pero solo cuando alguien lo desplegaba: el fallo aparecia tarde y en el
/// entorno equivocado. Aqui se comprueba en el momento de emitir.
/// </summary>
public sealed class InvoiceXmlPayloadTests : IDisposable
{
    private readonly BillingDatabaseFixture _fixture = new();

    public void Dispose() => _fixture.Dispose();

    [Fact]
    public async Task El_registro_emitted_guarda_el_xml_y_valida_contra_el_xsd()
    {
        var seeded = _fixture.Seed();
        using var scope = _fixture.CreateScope(seeded.TenantId);
        var db = _fixture.GetDb(scope);

        var invoiceId = await CreateDraftAsync(scope, seeded);

        var issuance = scope.ServiceProvider.GetRequiredService<IInvoiceIssuanceService>();
        var result = await issuance.IssueAsync(invoiceId, "XML", CancellationToken.None);

        Assert.True(result.IsSuccess);

        var record = await db.FiscalRecords.AsNoTracking()
            .SingleAsync(r => r.Id == result.Value.FiscalRecordId);

        Assert.False(string.IsNullOrWhiteSpace(record.XmlPayload),
            "La emision debe guardar el XML: un registro sin XML no se puede remitir.");

        // No basta con que no este vacio: tiene que pasar el esquema oficial.
        var validator = new AeatXsdValidator(Path.Combine(AppContext.BaseDirectory, "xsd"));
        var validation = validator.Validate(record.XmlPayload);

        Assert.True(validation.IsValid, validation.Summary);
    }

    [Fact]
    public async Task El_xml_guardado_lleva_el_sobre_de_remision_no_el_fragmento()
    {
        var seeded = _fixture.Seed();
        using var scope = _fixture.CreateScope(seeded.TenantId);
        var db = _fixture.GetDb(scope);

        var invoiceId = await CreateDraftAsync(scope, seeded);
        var issuance = scope.ServiceProvider.GetRequiredService<IInvoiceIssuanceService>();
        var result = await issuance.IssueAsync(invoiceId, "SOBRE", CancellationToken.None);

        var record = await db.FiscalRecords.AsNoTracking()
            .SingleAsync(r => r.Id == result.Value.FiscalRecordId);

        // La raiz del esquema es RegFactuSistemaFacturacion. Un RegistroAlta
        // suelto no valida, y es lo que se remitiria a la AEAT.
        Assert.Contains("RegFactuSistemaFacturacion", record.XmlPayload);
        Assert.Contains("Cabecera", record.XmlPayload);
        Assert.Contains("Huella", record.XmlPayload);
    }

    [Fact]
    public async Task La_huella_del_registro_coincide_con_la_del_xml()
    {
        var seeded = _fixture.Seed();
        using var scope = _fixture.CreateScope(seeded.TenantId);
        var db = _fixture.GetDb(scope);

        var invoiceId = await CreateDraftAsync(scope, seeded);
        var issuance = scope.ServiceProvider.GetRequiredService<IInvoiceIssuanceService>();
        var result = await issuance.IssueAsync(invoiceId, "HUELLA", CancellationToken.None);

        var record = await db.FiscalRecords.AsNoTracking()
            .SingleAsync(r => r.Id == result.Value.FiscalRecordId);

        // Si el XML y el registro__.__Hash__ divergieran, la AEAT rechazaria el
        // documento aunque cada uno por separado pareciera correcto.
        Assert.Contains(record.Hash, record.XmlPayload);
    }

    [Fact]
    public async Task Una_segunda_factura_encadena_con_la_anterior_en_el_xml()
    {
        var seeded = _fixture.Seed();
        using var scope = _fixture.CreateScope(seeded.TenantId);
        var db = _fixture.GetDb(scope);

        var issuance = scope.ServiceProvider.GetRequiredService<IInvoiceIssuanceService>();

        var firstId = await CreateDraftAsync(scope, seeded);
        var first = await issuance.IssueAsync(firstId, "CADENA-1", CancellationToken.None);

        var secondId = await CreateDraftAsync(scope, seeded);
        var second = await issuance.IssueAsync(secondId, "CADENA-2", CancellationToken.None);

        // SQLite no admite DateTimeOffset en ORDER BY: se ordena en memoria.
        var records = (await db.FiscalRecords.AsNoTracking()
                .Where(r => r.Id == first.Value.FiscalRecordId || r.Id == second.Value.FiscalRecordId)
                .ToListAsync())
            .OrderBy(r => r.GeneratedAt)
            .ToList();

        Assert.Equal(2, records.Count);

        var firstXml = records[0].XmlPayload;
        var secondXml = records[1].XmlPayload;

        Assert.Contains("PrimerRegistro", firstXml);

        // La segunda debe referenciar a la primera: si no, la trazabilidad se
        // rompe aunque las dos validen por separado.
        Assert.Contains("RegistroAnterior", secondXml);
        Assert.Contains(records[0].Hash, secondXml);
    }

    private async Task<Guid> CreateDraftAsync(
        IServiceScope scope,
        BillingDatabaseFixture.SeededCompany seeded)
    {
        var invoices = scope.ServiceProvider.GetRequiredService<IInvoiceApplicationService>();

        var draft = new CreateInvoiceRequest(
            seeded.CompanyId, seeded.CustomerId, new DateOnly(2024, 3, 15), "EUR",
            [new CreateInvoiceLineRequest("Consultoria", 2m, 50m, 21m)]);

        var created = await invoices.CreateDraftAsync(draft, CancellationToken.None);
        Assert.True(created.IsSuccess, created.IsFailure ? created.Errors[0].Code : "");

        return created.Value.Id;
    }
}