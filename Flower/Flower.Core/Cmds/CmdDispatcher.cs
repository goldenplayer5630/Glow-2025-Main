using Flower.Core.Abstractions.Commands;
using Flower.Core.Abstractions.Services;
using Flower.Core.Enums;
using Flower.Core.Models;
using Flower.Core.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Flower.Core.Cmds
{
    public class CmdDispatcher : ICmdDispatcher
    {
        private readonly Channel<CommandRequest> _queue = Channel.CreateUnbounded<CommandRequest>(new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });
        private readonly IProtocolClient _protocol;
        private readonly IFlowerStateService _state;

        public CmdDispatcher(IProtocolClient protocol, IFlowerStateService state)
        {
            _protocol = protocol;
            _state = state;
            _ = Task.Run(WorkerLoop);
        }

        public Task<CommandOutcome> EnqueueAsync(CommandRequest req, CancellationToken ct = default)
        {
            var tcs = new TaskCompletionSource<CommandOutcome>(TaskCreationOptions.RunContinuationsAsynchronously);
            _queue.Writer.TryWrite(req with
            {
                // attach a continuation object if you like; or keep a parallel dictionary
            });
            return tcs.Task;
        }

        private async Task WorkerLoop()
        {
            await foreach (var req in _queue.Reader.ReadAllAsync())
            {
                var corr = Guid.NewGuid();
                var env = new ProtocolEnvelope(corr, req.FlowerId, req.CommandId, BuildPayload(req.Args), ProtocolMessageType.Command);

                CommandOutcome outcome;
                try
                {
                    var ack = await _protocol.SendAndWaitAckAsync(env, req.AckTimeout);
                    outcome = ack.Type == ProtocolMessageType.Ack ? CommandOutcome.Acked :
                              CommandOutcome.Nacked;
                }
                catch (OperationCanceledException)
                {
                    outcome = CommandOutcome.Timeout;
                }
                catch
                {
                    outcome = CommandOutcome.Timeout; // treat errors as timeouts for state policy
                }

                // State transitions
                switch (outcome)
                {
                    case CommandOutcome.Acked:
                        if (req.StateOnAck != null) await _state.ApplyAsync(req.FlowerId, req.StateOnAck);
                        else await _state.TouchConnectionAsync(req.FlowerId, ConnectionStatus.Connected);
                        break;
                    case CommandOutcome.Timeout:
                        if (req.StateOnTimeout != null) await _state.ApplyAsync(req.FlowerId, req.StateOnTimeout);
                        else await _state.TouchConnectionAsync(req.FlowerId, ConnectionStatus.Degraded);
                        break;
                    case CommandOutcome.Nacked:
                        // optional: mark degraded or leave as-is
                        await _state.TouchConnectionAsync(req.FlowerId, ConnectionStatus.Degraded);
                        break;
                }

                // complete the task for EnqueueAsync(...) caller (omitted here for brevity)
            }
        }

        private static ReadOnlyMemory<byte> BuildPayload(IReadOnlyDictionary<string, object?> args)
        {
            // map args to your wire format
            // e.g., span writer, ushort durations, etc.
            throw new NotImplementedException();
        }
    }
}
