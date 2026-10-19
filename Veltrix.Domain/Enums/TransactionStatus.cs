using System;
using System.Collections.Generic;
using System.Text;

namespace Veltrix.Domain.Enums
{
    public enum TransactionStatus
    {
        Pending = 0,
        Completed = 1,
        Failed = 2,
        Reversed = 3
    }
}
