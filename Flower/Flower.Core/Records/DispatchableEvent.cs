using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flower.Core.Records
{
    public sealed record DispatchableEvent(
        long AtMs,                      // when to fire since loop start
        CommandRequest Request         // what to run (validated args, timeouts, state deltas)
    );
}
