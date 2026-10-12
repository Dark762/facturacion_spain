using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Facturacion.Application.Provisioning
{
    public sealed record BootstrapTenantResponse(
     Guid TenantId,
     Guid CompanyId,
     string AdminUserId
     );

    public sealed record CompanyResponse(
    Guid Id,
    Guid TenantId,
    string Nif,
    string LegalName
    );
}
