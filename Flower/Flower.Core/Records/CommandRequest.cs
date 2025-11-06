using Flower.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flower.Core.Records
{
    public sealed record CommandRequest(
        string BusId,
        int FlowerId,
        string CommandId,
        IReadOnlyDictionary<string, object?> Args,
        TimeSpan AckTimeout,
        IReadOnlyList<byte[]> Frames,
        Func<FlowerUnit, FlowerUnit>? StateOnAck,         // expected delta
        Func<FlowerUnit, FlowerUnit>? StateOnTimeout ,     // degrade, etc.
        Func<FlowerUnit, bool>? ShouldSkip = null

    );
}
