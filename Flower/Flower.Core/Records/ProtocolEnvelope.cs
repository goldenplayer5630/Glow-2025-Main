using Flower.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flower.Core.Records
{
    public sealed record ProtocolEnvelope(
        Guid CorrelationId,   // generated per request
        int FlowerId,
        string CommandId,

        IReadOnlyList<byte[]> Frames,
        ProtocolMessageType Type // Command, Ack, Nack, Event, Heartbeat...
    );
}
