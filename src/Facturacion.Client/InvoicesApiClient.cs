namespace Facturacion.Client;

/// <summary>Operaciones de facturas respaldadas por endpoints existentes de Facturacion.Api.</summary>
public sealed class InvoicesApiClient
{
    private readonly ApiClient _api;
    public InvoicesApiClient(ApiClient api) => _api = api;

    public Task<ApiResult<InvoiceDto>> GetAsync(Guid invoiceId, CancellationToken ct = default) =>
        _api.GetAsync<InvoiceDto>($"{ApiPaths.Invoices}/{invoiceId:D}", ct);

    public Task<ApiResult<InvoiceDto>> CreateDraftAsync(CreateInvoiceDto request, CancellationToken ct = default) =>
        _api.PostAsync<InvoiceDto>(ApiPaths.Invoices, request, ct);

    public Task<ApiResult<InvoiceIssueDto>> IssueAsync(Guid invoiceId, string series, CancellationToken ct = default) =>
        _api.PostAsync<InvoiceIssueDto>($"{ApiPaths.Invoices}/{invoiceId:D}/issue", new { series }, ct);
}

public sealed record CreateInvoiceDto(Guid CompanyId, Guid CustomerId, DateOnly IssueDate, string Currency, IReadOnlyList<CreateInvoiceLineDto> Lines);
public sealed record CreateInvoiceLineDto(string Description, decimal Quantity, decimal UnitPrice, decimal VatRate);
public sealed record InvoiceDto(Guid Id, Guid CompanyId, Guid CustomerId, string? Series, string? Number, decimal TaxableBase, decimal TaxAmount, decimal Total, string Status);
public sealed record InvoiceIssueDto(Guid InvoiceId, string InvoiceNumber, Guid FiscalRecordId);
