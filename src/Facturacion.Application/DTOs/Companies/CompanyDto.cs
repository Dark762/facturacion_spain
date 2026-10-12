namespace Facturacion.Application.DTOs.Companies;
public sealed record CompanyDto(Guid Id, string Nif, string LegalName, bool IsActive);
