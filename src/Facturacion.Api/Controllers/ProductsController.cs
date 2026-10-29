using Facturacion.Application.Interfaces;
using Facturacion.Domain.Entities;
using Facturacion.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Facturacion.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/companies/{companyId:guid}/products")]
public sealed class ProductsController : ControllerBase
{
    private readonly BillingDbContext _db;
    private readonly ICompanyAuthorizationService _authorization;
    private readonly ITenantContext _tenant;

    public ProductsController(BillingDbContext db, ICompanyAuthorizationService authorization, ITenantContext tenant)
    { _db = db; _authorization = authorization; _tenant = tenant; }

    public sealed record ProductResponse(Guid Id, Guid CompanyId, string Code, string Name,
        string Description, decimal UnitPrice, decimal VatRate, bool IsActive);
    public sealed record SaveProductRequest(string Code, string Name, string Description,
        decimal UnitPrice, decimal VatRate, bool IsActive);

    [HttpGet]
    public async Task<IActionResult> List(Guid companyId, CancellationToken ct)
    {
        if ((await _authorization.AuthorizeAsync(companyId, "catalog.read", ct)).IsFailure) return Forbid();
        var items = await _db.Products.AsNoTracking()
            .Where(x => x.TenantId == _tenant.TenantId && x.CompanyId == companyId)
            .OrderBy(x => x.Name)
            .Select(x => new ProductResponse(x.Id, x.CompanyId, x.Code, x.Name, x.Description, x.UnitPrice, x.VatRate, x.IsActive))
            .ToListAsync(ct);
        return Ok(items);
    }

    [HttpGet("{productId:guid}")]
    public async Task<IActionResult> Get(Guid companyId, Guid productId, CancellationToken ct)
    {
        if ((await _authorization.AuthorizeAsync(companyId, "catalog.read", ct)).IsFailure) return Forbid();
        var item = await Find(companyId, productId, ct);
        return item is null ? NotFound() : Ok(ToResponse(item));
    }

    [HttpPost]
    public async Task<IActionResult> Create(Guid companyId, [FromBody] SaveProductRequest request, CancellationToken ct)
    {
        if ((await _authorization.AuthorizeAsync(companyId, "catalog.write", ct)).IsFailure) return Forbid();
        if (!Valid(request)) return BadRequest("Datos de producto no válidos.");
        if (await Duplicate(companyId, request.Code.Trim(), null, ct)) return Conflict("El código ya existe en esta empresa.");
        var item = new Product { TenantId = _tenant.TenantId, CompanyId = companyId };
        Apply(item, request);
        _db.Products.Add(item);
        await _db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Get), new { companyId, productId = item.Id }, ToResponse(item));
    }

    [HttpPut("{productId:guid}")]
    public async Task<IActionResult> Update(Guid companyId, Guid productId, [FromBody] SaveProductRequest request, CancellationToken ct)
    {
        if ((await _authorization.AuthorizeAsync(companyId, "catalog.write", ct)).IsFailure) return Forbid();
        if (!Valid(request)) return BadRequest("Datos de producto no válidos.");
        var item = await Find(companyId, productId, ct);
        if (item is null) return NotFound();
        if (await Duplicate(companyId, request.Code.Trim(), productId, ct)) return Conflict("El código ya existe en esta empresa.");
        Apply(item, request);
        await _db.SaveChangesAsync(ct);
        return Ok(ToResponse(item));
    }

    [HttpDelete("{productId:guid}")]
    public async Task<IActionResult> Delete(Guid companyId, Guid productId, CancellationToken ct)
    {
        if ((await _authorization.AuthorizeAsync(companyId, "catalog.write", ct)).IsFailure) return Forbid();
        var item = await Find(companyId, productId, ct);
        if (item is null) return NotFound();
        _db.Products.Remove(item);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    private Task<Product?> Find(Guid companyId, Guid id, CancellationToken ct) =>
        _db.Products.FirstOrDefaultAsync(x => x.TenantId == _tenant.TenantId && x.CompanyId == companyId && x.Id == id, ct);

    private Task<bool> Duplicate(Guid companyId, string code, Guid? exclude, CancellationToken ct) =>
        _db.Products.AnyAsync(x => x.TenantId == _tenant.TenantId && x.CompanyId == companyId &&
            x.Code == code && (!exclude.HasValue || x.Id != exclude.Value), ct);

    private static bool Valid(SaveProductRequest r) =>
        !string.IsNullOrWhiteSpace(r.Code) && r.Code.Length <= 64 &&
        !string.IsNullOrWhiteSpace(r.Name) && r.Name.Length <= 250 &&
        (r.Description?.Length ?? 0) <= 1000 && r.UnitPrice >= 0 &&
        r.VatRate is 0m or 4m or 10m or 21m;

    private static void Apply(Product p, SaveProductRequest r)
    {
        p.Code = r.Code.Trim();
        p.Name = r.Name.Trim();
        p.Description = r.Description?.Trim() ?? "";
        p.UnitPrice = r.UnitPrice;
        p.VatRate = r.VatRate;
        p.IsActive = r.IsActive;
    }

    private static ProductResponse ToResponse(Product p) =>
        new(p.Id, p.CompanyId, p.Code, p.Name, p.Description, p.UnitPrice, p.VatRate, p.IsActive);
}
