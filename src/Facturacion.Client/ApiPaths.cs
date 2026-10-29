namespace Facturacion.Client;

/// <summary>Rutas de los servicios HTTP compartidos.</summary>
public static class ApiPaths
{
    public static string Products(Guid companyId) => $"api/companies/{companyId:D}/products";
    public static string Customers(Guid companyId) => $"api/companies/{companyId:D}/customers";
    public const string Invoices = "api/invoices";
    public const string Plans = "api/saas/plans";
    public const string Register = "api/saas/register";
    public const string VerifyEmail = "api/saas/verify-email";
    public const string Company = "api/saas/company";
    public const string Checkout = "api/saas/checkout";
    public const string Login = "api/auth/login";
}
