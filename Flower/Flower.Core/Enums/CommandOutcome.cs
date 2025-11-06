using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flower.Core.Enums
{
    public enum CommandOutcome { 
        Acked, 
        Nacked, 
        Timeout,
        SkippedNotConnected,
        SkippedNoOp,
        BusNotConnected
    }
}
