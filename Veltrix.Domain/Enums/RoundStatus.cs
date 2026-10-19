using System;
using System.Collections.Generic;
using System.Text;

namespace Veltrix.Domain.Enums
{
    public enum RoundStatus
    {
        Waiting = 0,
        BettingOpen = 1,
        BettingClosed = 2,
        Running = 3,
        Crashed = 4,
        Finished = 5,
        Cancelled = 6
    }
}
