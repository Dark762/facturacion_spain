namespace Facturacion.Application.Provisioning;
public interface IProvisioningService
{
    Task<ProvisioningResult> ProvisionAsync(ProvisioningRequest request, CancellationToken ct = default);
    Task<BootstrapTenantResponse> BootstrapAsync(BootstrapTenantRequest request, CancellationToken ct);
    Task<CompanyResponse> CreateCompanyAsync(Guid tenantId, CreateCompanyRequest request, CancellationToken ct);
    Task<string> AddTenantUserAsync(Guid tenantId, CreateTenantUserRequest request, CancellationToken ct);
    Task AssignCompanyUserAsync(Guid tenantId, Guid companyId, AssignCompanyUserRequest request, CancellationToken ct);
}
