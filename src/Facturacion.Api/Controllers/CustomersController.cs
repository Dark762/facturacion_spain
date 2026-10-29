using Facturacion.Application.Interfaces;
using Facturacion.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Facturacion.Api.Controllers;

/// <summary>Consultas de clientes limitadas al tenant del JWT y a la empresa autorizada.</summary>
[ApiController]
[Authorize]
[Route("api/companies/{companyId:guid}/customers")]
public sealed class CustomersController : ControllerBase
{
    private readonly BillingDbContext _db;
    private readonly ICompanyAuthorizationService _authorization;
    private readonly ITenantContext _tenant;

    public CustomersController(BillingDbContext db, ICompanyAuthorizationService authorization, ITenantContext tenant)
    {
        _db = db;
        _authorization = authorization;
        _tenant = tenant;
    }

    public sealed record CustomerResponse(Guid Id, Guid CompanyId, string TaxId, string LegalName,
        string Address, string PostalCode, string City, string CountryCode, string? Email, bool IsBusiness);

    [HttpGet]
    public async Task<IActionResult> List(Guid companyId, CancellationToken ct)
    {
        var access = await _authorization.AuthorizeAsync(companyId, "invoices.read", ct);
        if (access.IsFailure) return Forbid();
        var customers = await _db.Customers.AsNoTracking()
            .Where(x => x.TenantId == _tenant.TenantId && x.CompanyId == companyId)
            .OrderBy(x => x.LegalName)
            .Select(x => new CustomerResponse(x.Id, x.CompanyId, x.TaxId, x.LegalName,
                x.Address, x.PostalCode, x.City, x.CountryCode, x.Email, x.IsBusiness))
            .ToListAsync(ct);
        return Ok(customers);
    }

    [HttpGet("{customerId:guid}")]
    public async Task<IActionResult> Get(Guid companyId, Guid customerId, CancellationToken ct)
    {
        var access = await _authorization.AuthorizeAsync(companyId, "invoices.read", ct);
        if (access.IsFailure) return Forbid();
        var customer = await _db.Customers.AsNoTracking()
            .Where(x => x.TenantId == _tenant.TenantId && x.CompanyId == companyId && x.Id == customerId)
            .Select(x => new CustomerResponse(x.Id, x.CompanyId, x.TaxId, x.LegalName,
                x.Address, x.PostalCode, x.City, x.CountryCode, x.Email, x.IsBusiness))
            .FirstOrDefaultAsync(ct);
        return customer is null ? NotFound() : Ok(customer);
    }
}
