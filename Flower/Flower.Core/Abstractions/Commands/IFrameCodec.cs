using Flower.Core.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flower.Core.Abstractions.Commands
{
    public interface IFrameCodec
    {
        byte[] Encode(ProtocolEnvelope env); // Add SOF/EOF/len/CRC here
        bool TryDecode(ReadOnlySpan<byte> frame, out ProtocolEnvelope env);
    }
}
