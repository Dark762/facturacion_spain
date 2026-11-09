using Facturacion.Api.Responses;
using Facturacion.Application.Interfaces;
using Facturacion.Domain.Entities;
using Facturacion.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net.Mail;

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
    public async Task<IActionResult> List(Guid companyId, CancellationToken ct, [FromQuery] string? search = null,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var access = await _authorization.AuthorizeAsync(companyId, "catalog.read", ct);
        if (access.IsFailure) return this.ApiFailure<object>(403, "FORBIDDEN", "No tienes permisos para esta operación.");
        if (page < 1 || pageSize is < 1 or > 100 || search?.Length > 120)
            return this.ApiFailure<object>(400, "CUSTOMER_QUERY_INVALID", "Página, tamaño o búsqueda no válidos.");
        var query = _db.Customers.AsNoTracking()
            .Where(x => x.TenantId == _tenant.TenantId && x.CompanyId == companyId);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x => x.LegalName.Contains(term) || x.TaxId.Contains(term));
        }
        var customers = await query
            .OrderBy(x => x.LegalName).ThenBy(x => x.Id)
            .Skip((int)Math.Min((long)(page - 1) * pageSize, int.MaxValue)).Take(pageSize)
            .Select(x => new CustomerResponse(x.Id, x.CompanyId, x.TaxId, x.LegalName,
                x.Address, x.PostalCode, x.City, x.CountryCode, x.Email, x.IsBusiness))
            .ToListAsync(ct);
        return this.ApiOk(customers);
    }

    [HttpGet("{customerId:guid}")]
    public async Task<IActionResult> Get(Guid companyId, Guid customerId, CancellationToken ct)
    {
        var access = await _authorization.AuthorizeAsync(companyId, "invoices.read", ct);
        if (access.IsFailure) return this.ApiFailure<object>(403, "FORBIDDEN", "No tienes permisos para esta operación.");
        var customer = await _db.Customers.AsNoTracking()
            .Where(x => x.TenantId == _tenant.TenantId && x.CompanyId == companyId && x.Id == customerId)
            .Select(x => new CustomerResponse(x.Id, x.CompanyId, x.TaxId, x.LegalName,
                x.Address, x.PostalCode, x.City, x.CountryCode, x.Email, x.IsBusiness))
            .FirstOrDefaultAsync(ct);
        return customer is null ? this.ApiFailure<object>(404, "CUSTOMER_NOT_FOUND", "Cliente no encontrado.") : this.ApiOk(customer);
    }
    public sealed record SaveCustomerRequest(string TaxId, string LegalName, string Address,
        string PostalCode, string City, string CountryCode, string? Email, bool IsBusiness);

    [HttpPost]
    public async Task<IActionResult> Create(Guid companyId, [FromBody] SaveCustomerRequest request, CancellationToken ct)
    {
        var access = await _authorization.AuthorizeAsync(companyId, "catalog.write", ct);
        if (access.IsFailure) return this.ApiFailure<object>(403, "FORBIDDEN", "No tienes permisos para esta operación.");
        if (!IsValid(request)) return this.ApiFailure<object>(400, "CUSTOMER_INVALID", "Datos de cliente no válidos.");
        if (await HasDuplicateTaxIdAsync(companyId, request.TaxId.Trim(), null, ct))
            return this.ApiFailure<object>(409, "CUSTOMER_TAX_ID_DUPLICATE", "Ya existe un cliente con esta identificación fiscal en la empresa.");
        if (await _db.Customers.AnyAsync(x => x.TenantId == _tenant.TenantId && x.CompanyId == companyId && x.TaxId == request.TaxId.Trim(), ct))
            return this.ApiFailure<object>(409, "CUSTOMER_TAXID_EXISTS", "Ya existe un cliente con esa identificación fiscal.");
        var entity = new Customer { TenantId = _tenant.TenantId, CompanyId = companyId };
        Apply(entity, request);
        _db.Customers.Add(entity);
        await _db.SaveChangesAsync(ct);
        return this.ApiCreated(Url.Action(nameof(Get), new { companyId, customerId = entity.Id }) ?? $"/api/companies/{companyId}/customers/{entity.Id}", ToResponse(entity), "Cliente creado correctamente.", "CUSTOMER_CREATED");
    }

    [HttpPut("{customerId:guid}")]
    public async Task<IActionResult> Update(Guid companyId, Guid customerId, [FromBody] SaveCustomerRequest request, CancellationToken ct)
    {
        var access = await _authorization.AuthorizeAsync(companyId, "catalog.write", ct);
        if (access.IsFailure) return this.ApiFailure<object>(403, "FORBIDDEN", "No tienes permisos para esta operación.");
        if (!IsValid(request)) return this.ApiFailure<object>(400, "CUSTOMER_INVALID", "Datos de cliente no válidos.");
        var entity = await _db.Customers.FirstOrDefaultAsync(x => x.TenantId == _tenant.TenantId && x.CompanyId == companyId && x.Id == customerId, ct);
        if (entity is null) return this.ApiFailure<object>(404, "CUSTOMER_NOT_FOUND", "Cliente no encontrado.");
        if (await HasDuplicateTaxIdAsync(companyId, request.TaxId.Trim(), customerId, ct))
            return this.ApiFailure<object>(409, "CUSTOMER_TAX_ID_DUPLICATE", "Ya existe otro cliente con esta identificación fiscal en la empresa.");
        if (await _db.Customers.AnyAsync(x => x.TenantId == _tenant.TenantId && x.CompanyId == companyId && x.Id != customerId && x.TaxId == request.TaxId.Trim(), ct))
            return this.ApiFailure<object>(409, "CUSTOMER_TAXID_EXISTS", "Ya existe un cliente con esa identificación fiscal.");
        Apply(entity, request);
        await _db.SaveChangesAsync(ct);
        return this.ApiOk(ToResponse(entity), "Cliente actualizado correctamente.");
    }

    [HttpDelete("{customerId:guid}")]
    public async Task<IActionResult> Delete(Guid companyId, Guid customerId, CancellationToken ct)
    {
        var access = await _authorization.AuthorizeAsync(companyId, "catalog.write", ct);
        if (access.IsFailure) return this.ApiFailure<object>(403, "FORBIDDEN", "No tienes permisos para esta operación.");
        var entity = await _db.Customers.FirstOrDefaultAsync(x => x.TenantId == _tenant.TenantId && x.CompanyId == companyId && x.Id == customerId, ct);
        if (entity is null) return this.ApiFailure<object>(404, "CUSTOMER_NOT_FOUND", "Cliente no encontrado.");
        // No se eliminan clientes que puedan estar referenciados por facturas.
        if (await _db.Invoices.AnyAsync(x => x.CustomerId == customerId && x.CompanyId == companyId, ct))
            return this.ApiFailure<object>(409, "CUSTOMER_HAS_INVOICES", "No se puede eliminar un cliente con facturas.");
        _db.Customers.Remove(entity);
        await _db.SaveChangesAsync(ct);
        return this.ApiOk<object?>(null, "Cliente eliminado correctamente.");
    }

    private Task<bool> HasDuplicateTaxIdAsync(Guid companyId, string taxId, Guid? excludeId, CancellationToken ct) =>
        _db.Customers.AsNoTracking().AnyAsync(x => x.TenantId == _tenant.TenantId &&
            x.CompanyId == companyId && x.TaxId == taxId && (!excludeId.HasValue || x.Id != excludeId.Value), ct);

    private static bool IsValid(SaveCustomerRequest r) =>
        !string.IsNullOrWhiteSpace(r.TaxId) && r.TaxId.Length <= 20 &&
        !string.IsNullOrWhiteSpace(r.LegalName) && r.LegalName.Length <= 250 &&
        (r.Address?.Length ?? 0) <= 500 && (r.PostalCode?.Length ?? 0) <= 16 &&
        (r.City?.Length ?? 0) <= 120 && !string.IsNullOrWhiteSpace(r.CountryCode) &&
        r.CountryCode.Length == 2 && r.CountryCode.All(char.IsLetter) &&
        (r.Email?.Length ?? 0) <= 254 && IsValidEmail(r.Email);

    private static bool IsValidEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email)) return true;
        try { return new MailAddress(email).Address == email.Trim(); }
        catch (FormatException) { return false; }
    }

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
