using Flower.Core.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flower.Core.Abstractions.Commands
{
    public interface IProtocolClient
    {
        Task<ProtocolEnvelope> SendAndWaitAckAsync(
            ProtocolEnvelope command,
            TimeSpan timeout,
            CancellationToken ct = default);
    }
}
