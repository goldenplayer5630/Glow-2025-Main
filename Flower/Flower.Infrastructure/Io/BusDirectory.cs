using Flower.Core.Abstractions.Commands;
using Flower.Core.Records;
using Flower.Infrastructure.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flower.Infrastructure.Io
{
    public sealed class BusDirectory : IBusDirectory
    {
        private readonly IFrameCodec _codec;
        private readonly Dictionary<string, (ITransport transport, IProtocolClient protocol)> _buses = new();

        public BusDirectory(IFrameCodec codec) => _codec = codec;

        public async Task OpenAsync(IEnumerable<BusConfig> configs)
        {
            foreach (var cfg in configs)
            {
                var transport = new SerialPortTransport();
                await transport.OpenAsync(cfg.Port, cfg.Baud);
                var protocol = new ProtocolClient(transport, _codec);
                _buses[cfg.BusId] = (transport, protocol);
            }
        }

        public IProtocolClient GetProtocol(string busId)
            => _buses.TryGetValue(busId, out var x) ? x.protocol
               : throw new KeyNotFoundException($"Bus '{busId}' not found.");

        public async ValueTask DisposeAsync()
        {
            foreach (var (_, (t, p)) in _buses)
            {
                await p.DisposeAsync();
                await t.DisposeAsync();
            }
            _buses.Clear();
        }
    }
}
