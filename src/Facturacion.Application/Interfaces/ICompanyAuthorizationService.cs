using Facturacion.Application.Common.Results;
namespace Facturacion.Application.Interfaces;
public interface ICompanyAuthorizationService
{
    Task<Result> AuthorizeAsync(Guid companyId, string permission, CancellationToken ct);
}
