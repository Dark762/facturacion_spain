namespace Facturacion.Application.Provisioning;

// RegistrationId debe coincidir con RegistrationIntent.Id, creado antes del Checkout.
public sealed record ProvisioningRequest(
    Guid RegistrationId,
    string UserId,
    string TenantName,
    string CompanyName,
    string TaxId,
    string Address,
    string PostalCode,
    string City,
    string Province);

public sealed record ProvisioningResult(Guid TenantId, Guid CompanyId, bool AlreadyProvisioned);

public sealed record CreateCompanyRequest(
    string Nif,
    string LegalName,
    string Address,
    string PostalCode,
    string City,
    string Province,
    string? TradeName,
    string? Email
);

public sealed record CreateTenantUserRequest(
    string Email,
    string Password,
    string DisplayName
);

public sealed record AssignCompanyUserRequest(
    string Email,
    string Role
);

public sealed record BootstrapTenantRequest(
    string TenantName,
    string Nif,
    string LegalName,
    string Address,
    string PostalCode,
    string City,
    string Province,
    string AdminEmail,
    string AdminPassword,
    string AdminDisplayName
);