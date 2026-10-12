using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Facturacion.Domain.Enums
{
    public enum InvoiceType
    {
        F1 = 1,  // Factura completa
        F2 = 2,  // Factura simplificada
        F3 = 3,  // Sustitutiva de simplificadas
        R1 = 11, // Rectificativa art. 80.1, 80.2, 80.6
        R2 = 12, // Rectificativa art. 80.3
        R3 = 13, // Rectificativa art. 80.4
        R4 = 14, // Otras rectificativas
        R5 = 15  // Rectificativa de simplificadas
    }
}
