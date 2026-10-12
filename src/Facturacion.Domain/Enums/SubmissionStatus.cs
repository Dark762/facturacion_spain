using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Facturacion.Domain.Enums
{
    public enum SubmissionStatus
    {
        Pending = 0,
        Processing = 1,
        Accepted = 2,
        AcceptedWithErrors = 3,
        Rejected = 4,
        RetryPending = 5,
        Unknown = 6
    }
}
