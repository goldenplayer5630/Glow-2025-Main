using Flower.Core.Abstractions.Commands;
using Flower.Core.Enums;
using Flower.Core.Records;
using Flower.Infrastructure.Protocol;

namespace Flower.Infrastructure.Io
{
    public sealed class SerialBusClient : IBusClient
    {
        private readonly ITransport _transport;
        private readonly IProtocolClient _protocol;

        public bool IsOpen => _transport.IsOpen;

        public SerialBusClient(ITransport transport, IProtocolClient protocol)
        {
            _transport = transport;
            _protocol = protocol;
        }

        public async Task<CommandOutcome> SendAsync(CommandRequest r, CancellationToken ct = default)
        {
            try
            {
                var corr = Guid.NewGuid();

                var env = new ProtocolEnvelope(
                    corr,
                    r.FlowerId,
                    r.CommandId,
                    r.Frames,
                    ProtocolMessageType.Command);

                var ack = await _protocol.SendAndWaitAckAsync(env, r.AckTimeout);

                if (r.FlowerId == 0)
                    return CommandOutcome.Timeout;

                return ack.Type == ProtocolMessageType.Ack
                    ? CommandOutcome.Acked
                    : CommandOutcome.Nacked;
            }
            catch (OperationCanceledException)
            {
                // keep your existing semantics
                return CommandOutcome.Acked;
            }
            catch
            {
                return CommandOutcome.Timeout;
            }
        }

        public async ValueTask DisposeAsync()
        {
            try { await _protocol.DisposeAsync(); } catch { }
            try { await _transport.DisposeAsync(); } catch { }
        }
    }
}
