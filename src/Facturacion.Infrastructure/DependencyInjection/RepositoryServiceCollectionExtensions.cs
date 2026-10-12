using Facturacion.Application.Interfaces;
using Facturacion.Infrastructure.Persistence;
using Facturacion.Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace Facturacion.Infrastructure.DependencyInjection;
public static class RepositoryServiceCollectionExtensions
{
    public static IServiceCollection AddBillingRepositories(this IServiceCollection services)
    {
        services.AddScoped<ICompanyRepository, CompanyRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IInvoiceRepository, InvoiceRepository>();
        services.AddScoped<IInvoiceLineRepository, InvoiceLineRepository>();
        services.AddScoped<IInvoiceTaxBreakdownRepository, InvoiceTaxBreakdownRepository>();
        services.AddScoped<IInvoiceSequenceRepository, InvoiceSequenceRepository>();
        services.AddScoped<IFiscalChainRepository, FiscalChainRepository>();
        services.AddScoped<IFiscalRecordRepository, FiscalRecordRepository>();
        services.AddScoped<IAeatSubmissionRepository, AeatSubmissionRepository>();
        services.AddScoped<IElectronicDocumentRepository, ElectronicDocumentRepository>();
        services.AddScoped<IB2BTransmissionRepository, B2BTransmissionRepository>();
        services.AddScoped<IInvoiceStatusEventRepository, InvoiceStatusEventRepository>();
        services.AddScoped<IInvoicePaymentRepository, InvoicePaymentRepository>();
        services.AddScoped<IOutboxMessageRepository, OutboxMessageRepository>();
        services.AddScoped<IAuditEventRepository, AuditEventRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        return services;
    }
}
