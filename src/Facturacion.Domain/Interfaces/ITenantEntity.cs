using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Facturacion.Domain.Interfaces
{
    public interface ITenantEntity
    {
        public Guid TenantId { get; set; }
    }
}
