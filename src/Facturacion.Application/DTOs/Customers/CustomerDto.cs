namespace Facturacion.Application.DTOs.Customers;
public sealed record CustomerDto(Guid Id, Guid CompanyId, string TaxId, string LegalName);
