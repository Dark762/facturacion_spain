using Facturacion.Application.Interfaces;
using Facturacion.Application.Services;
using Microsoft.Extensions.DependencyInjection;


namespace Facturacion.Application.DependencyInjection;
public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddBillingApplication(this IServiceCollection services)
    {
        services.AddScoped<IInvoiceApplicationService, InvoiceApplicationService>();
        services.AddScoped<IInvoiceIssuanceService, InvoiceIssuanceService>();
        services.AddScoped<IFiscalRecordService, FiscalRecordService>();
        services.AddScoped<IAeatSubmissionService, AeatSubmissionService>();
        services.AddScoped<IB2BInvoiceService, B2BInvoiceService>();
        return services;
    }
}
