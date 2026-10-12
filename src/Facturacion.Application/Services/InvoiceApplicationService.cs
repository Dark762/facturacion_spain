using Facturacion.Application.Common.Results;
using Facturacion.Application.Common.Errors;
using Facturacion.Application.DTOs.Invoices;
using Facturacion.Application.Interfaces;
using Facturacion.Domain.Entities;
using Facturacion.Domain.Enums;

namespace Facturacion.Application.Services;

public sealed class InvoiceApplicationService : IInvoiceApplicationService
{
    private readonly IInvoiceRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantContext _tenant;
    private readonly ICompanyAuthorizationService _authorization;

    public InvoiceApplicationService(
        IInvoiceRepository repository, IUnitOfWork unitOfWork, ITenantContext tenant,
        ICompanyAuthorizationService authorization)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _tenant = tenant;
        _authorization = authorization;
    }

    public async Task<Result<InvoiceDto>> CreateDraftAsync(
        CreateInvoiceRequest request, CancellationToken ct)
    {
        var access = await _authorization.AuthorizeAsync(request.CompanyId, "invoices.create", ct);
        if (access.IsFailure) return Result<InvoiceDto>.Failure(access.Errors.ToArray());

        if (request.Lines is null || request.Lines.Count == 0)
            return Result<InvoiceDto>.Failure(ApplicationErrors.Invalid(
                "INVOICE_LINES_REQUIRED", "Se requiere al menos una línea.", "lines"));

        if (!string.Equals(request.Currency, "EUR", StringComparison.OrdinalIgnoreCase))
            return Result<InvoiceDto>.Failure(ApplicationErrors.Invalid(
                "CURRENCY_NOT_SUPPORTED", "Este caso de uso solo admite EUR.", "currency"));

        if (request.IssueDate == default)
            return Result<InvoiceDto>.Failure(ApplicationErrors.Invalid(
                "ISSUE_DATE_REQUIRED", "La fecha de factura es obligatoria.", "issueDate"));

        if (request.Lines.Any(x => string.IsNullOrWhiteSpace(x.Description)
            || x.Quantity <= 0 || x.UnitPrice < 0 || x.VatRate is not (0m or 4m or 10m or 21m)))
            return Result<InvoiceDto>.Failure(ApplicationErrors.Invalid(
                "INVALID_INVOICE_LINE", "Línea inválida o tipo de IVA no admitido en este ejemplo.", "lines"));

        var company = await _repository.GetCompanyAsync(request.CompanyId, ct);
        if (company is null || company.TenantId != _tenant.TenantId || !company.IsActive)
            return Result<InvoiceDto>.Failure(ApplicationErrors.NotFound("Empresa"));

        var customer = await _repository.GetCustomerAsync(request.CompanyId, request.CustomerId, ct);
        if (customer is null || customer.TenantId != _tenant.TenantId)
            return Result<InvoiceDto>.Failure(ApplicationErrors.NotFound("Cliente"));

        var invoice = new Invoice
        {
            TenantId = _tenant.TenantId,
            CompanyId = company.Id,
            CustomerId = customer.Id,
            IssueDate = request.IssueDate,
            Currency = "EUR",
            Type = InvoiceType.F1,
            Status = InvoiceStatus.Draft,
            IssuerNif = company.Nif,
            IssuerName = company.LegalName,
            CustomerTaxId = customer.TaxId,
            CustomerName = customer.LegalName
        };

        int position = 0;
        foreach (var line in request.Lines)
        {
            // Ejemplo de borradores: redondeo por línea. No es motor fiscal completo.
            var net = decimal.Round(line.Quantity * line.UnitPrice, 2, MidpointRounding.AwayFromZero);
            var vat = decimal.Round(net * line.VatRate / 100m, 2, MidpointRounding.AwayFromZero);
            invoice.Lines.Add(new InvoiceLine
            {
                TenantId = _tenant.TenantId,
                InvoiceId = invoice.Id,
                LineNumber = ++position,
                Description = line.Description.Trim(),
                Quantity = line.Quantity,
                UnitPrice = line.UnitPrice,
                VatRate = line.VatRate,
                NetAmount = net,
                VatAmount = vat,
                TotalAmount = net + vat
            });
        }

        foreach (var group in invoice.Lines.GroupBy(x => x.VatRate))
        {
            invoice.TaxBreakdowns.Add(new InvoiceTaxBreakdown
            {
                TenantId = _tenant.TenantId,
                InvoiceId = invoice.Id,
                TaxCode = "IVA",
                TaxRate = group.Key,
                TaxableBase = group.Sum(x => x.NetAmount),
                TaxAmount = group.Sum(x => x.VatAmount)
            });
        }

        invoice.TaxableBase = invoice.Lines.Sum(x => x.NetAmount);
        invoice.TaxAmount = invoice.Lines.Sum(x => x.VatAmount);
        invoice.Total = invoice.TaxableBase + invoice.TaxAmount;

        await _repository.AddAsync(invoice, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<InvoiceDto>.Success(ToDto(invoice));
    }

    public async Task<Result<InvoiceDto>> GetByIdAsync(Guid invoiceId, CancellationToken ct)
    {
        var invoice = await _repository.GetByIdAsync(invoiceId, ct);
        if (invoice is null || invoice.TenantId != _tenant.TenantId)
            return Result<InvoiceDto>.Failure(ApplicationErrors.NotFound("Factura"));

        var access = await _authorization.AuthorizeAsync(invoice.CompanyId, "invoices.read", ct);
        if (access.IsFailure) return Result<InvoiceDto>.Failure(access.Errors.ToArray());
        return Result<InvoiceDto>.Success(ToDto(invoice));
    }

    private static InvoiceDto ToDto(Invoice invoice) => new(
        invoice.Id, invoice.CompanyId, invoice.CustomerId, invoice.Series,
        invoice.Number, invoice.TaxableBase, invoice.TaxAmount,
        invoice.Total, invoice.Status.ToString());
}
