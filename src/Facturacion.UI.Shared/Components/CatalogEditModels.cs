using System.ComponentModel.DataAnnotations;
using Facturacion.Client;

namespace Facturacion.UI.Shared.Components;

public sealed class CustomerEditModel
{
    [Required, StringLength(20)] public string TaxId { get; set; } = "";
    [Required, StringLength(250)] public string LegalName { get; set; } = "";
    [StringLength(500)] public string Address { get; set; } = "";
    [StringLength(16)] public string PostalCode { get; set; } = "";
    [StringLength(120)] public string City { get; set; } = "";
    [Required, StringLength(2, MinimumLength = 2)] public string CountryCode { get; set; } = "ES";
    [EmailAddress, StringLength(254)] public string? Email { get; set; }
    public bool IsBusiness { get; set; } = true;

    public static CustomerEditModel From(CustomerDto c) => new()
    {
        TaxId = c.TaxId, LegalName = c.LegalName, Address = c.Address,
        PostalCode = c.PostalCode, City = c.City, CountryCode = c.CountryCode,
        Email = c.Email, IsBusiness = c.IsBusiness
    };
}

public sealed class ProductEditModel
{
    [Required, StringLength(64)] public string Code { get; set; } = "";
    [Required, StringLength(250)] public string Name { get; set; } = "";
    [StringLength(1000)] public string Description { get; set; } = "";
    [Range(0, 9999999999999999d)] public decimal UnitPrice { get; set; }
    [Range(0, 21)] public decimal VatRate { get; set; } = 21m;
    public bool IsActive { get; set; } = true;

    public static ProductEditModel From(ProductDto p) => new()
    {
        Code = p.Code, Name = p.Name, Description = p.Description,
        UnitPrice = p.UnitPrice, VatRate = p.VatRate, IsActive = p.IsActive
    };
}
