using Facturacion.Application.Interfaces;
using Facturacion.Domain.Entities;
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
        var access = await _authorization.AuthorizeAsync(companyId, "catalog.read", ct);
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
    public sealed record SaveCustomerRequest(string TaxId, string LegalName, string Address,
        string PostalCode, string City, string CountryCode, string? Email, bool IsBusiness);

    [HttpPost]
    public async Task<IActionResult> Create(Guid companyId, [FromBody] SaveCustomerRequest request, CancellationToken ct)
    {
        var access = await _authorization.AuthorizeAsync(companyId, "catalog.write", ct);
        if (access.IsFailure) return Forbid();
        if (!IsValid(request)) return BadRequest("Datos de cliente no válidos.");
        var entity = new Customer { TenantId = _tenant.TenantId, CompanyId = companyId };
        Apply(entity, request);
        _db.Customers.Add(entity);
        await _db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Get), new { companyId, customerId = entity.Id }, ToResponse(entity));
    }

    [HttpPut("{customerId:guid}")]
    public async Task<IActionResult> Update(Guid companyId, Guid customerId, [FromBody] SaveCustomerRequest request, CancellationToken ct)
    {
        var access = await _authorization.AuthorizeAsync(companyId, "catalog.write", ct);
        if (access.IsFailure) return Forbid();
        if (!IsValid(request)) return BadRequest("Datos de cliente no válidos.");
        var entity = await _db.Customers.FirstOrDefaultAsync(x => x.TenantId == _tenant.TenantId && x.CompanyId == companyId && x.Id == customerId, ct);
        if (entity is null) return NotFound();
        Apply(entity, request);
        await _db.SaveChangesAsync(ct);
        return Ok(ToResponse(entity));
    }

    [HttpDelete("{customerId:guid}")]
    public async Task<IActionResult> Delete(Guid companyId, Guid customerId, CancellationToken ct)
    {
        var access = await _authorization.AuthorizeAsync(companyId, "catalog.write", ct);
        if (access.IsFailure) return Forbid();
        var entity = await _db.Customers.FirstOrDefaultAsync(x => x.TenantId == _tenant.TenantId && x.CompanyId == companyId && x.Id == customerId, ct);
        if (entity is null) return NotFound();
        // No se eliminan clientes que puedan estar referenciados por facturas.
        if (await _db.Invoices.AnyAsync(x => x.CustomerId == customerId && x.CompanyId == companyId, ct))
            return Conflict("No se puede eliminar un cliente con facturas.");
        _db.Customers.Remove(entity);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    private static bool IsValid(SaveCustomerRequest r) =>
        !string.IsNullOrWhiteSpace(r.TaxId) && r.TaxId.Length <= 20 &&
        !string.IsNullOrWhiteSpace(r.LegalName) && r.LegalName.Length <= 250 &&
        (r.Address?.Length ?? 0) <= 500 && (r.PostalCode?.Length ?? 0) <= 16 &&
        (r.City?.Length ?? 0) <= 120 && !string.IsNullOrWhiteSpace(r.CountryCode) &&
        r.CountryCode.Length == 2 && (r.Email?.Length ?? 0) <= 254;

    private static void Apply(Customer c, SaveCustomerRequest r)
    {
        c.TaxId = r.TaxId.Trim();
        c.LegalName = r.LegalName.Trim();
        c.Address = r.Address?.Trim() ?? "";
        c.PostalCode = r.PostalCode?.Trim() ?? "";
        c.City = r.City?.Trim() ?? "";
        c.CountryCode = r.CountryCode.Trim().ToUpperInvariant();
        c.Email = r.Email?.Trim();
        c.IsBusiness = r.IsBusiness;
    }

    private static CustomerResponse ToResponse(Customer c) =>
        new(c.Id, c.CompanyId, c.TaxId, c.LegalName, c.Address, c.PostalCode,
            c.City, c.CountryCode, c.Email, c.IsBusiness);

}
