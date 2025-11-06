using Flower.Core.Abstractions.Commands;
using Flower.Core.Enums;
using Flower.Core.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Flower.Infrastructure.Protocol
{
    public sealed class JsonLineCodec : IFrameCodec
    {
        private sealed class Wire
        {
            public Guid CorrelationId { get; set; }
            public int FlowerId { get; set; }
            public string CommandId { get; set; } = string.Empty;
            public ProtocolMessageType Type { get; set; }
            public byte[]? Payload { get; set; }
        }

        public bool TryDecode(ReadOnlySpan<byte> frame, out ProtocolEnvelope env)
        {
            try
            {
                var json = Encoding.UTF8.GetString(frame);
                var wire = JsonSerializer.Deserialize<Wire>(json);
                if (wire is null) { env = default!; return false; }

                env = new ProtocolEnvelope(
                    wire.CorrelationId,
                    wire.FlowerId,
                    wire.CommandId,
                    new List<byte[]> { wire.Payload ?? Array.Empty<byte>() },
                    wire.Type);
                return true;
            }
            catch
            {
                env = default!;
                return false;
            }
        }
    }
}
