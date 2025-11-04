using Flower.Core.Abstractions.Commands;
using Flower.Core.Abstractions.Services;
using Flower.Core.Enums;
using Flower.Core.Models;
using Flower.Core.Records;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Flower.Core.Cmds
{
    public sealed class CmdDispatcher : ICmdDispatcher, IAsyncDisposable
    {
        private readonly IBusDirectory _buses;
        private readonly IFlowerStateService _state;
        private readonly IFlowerService _flowers;

        private readonly ConcurrentDictionary<(string BusId, int FlowerId), Channel<CommandWork>> _queues = new();
        private readonly ConcurrentDictionary<(string BusId, int FlowerId), Task> _workers = new();

        public CmdDispatcher(IBusDirectory buses, IFlowerStateService state, IFlowerService flowers)
        {
            _buses = buses;
            _state = state;
            _flowers = flowers;
        }

        private sealed class CommandWork
        {
            public required CommandRequest Req { get; init; }
            public required TaskCompletionSource<CommandOutcome> Tcs { get; init; }
        }

        public Task<CommandOutcome> EnqueueAsync(CommandRequest req, CancellationToken ct = default)
        {
            var key = (req.BusId, req.FlowerId);
            var ch = _queues.GetOrAdd(key, _ =>
                Channel.CreateUnbounded<CommandWork>(new UnboundedChannelOptions { SingleReader = true, SingleWriter = false }));

            var tcs = new TaskCompletionSource<CommandOutcome>(TaskCreationOptions.RunContinuationsAsynchronously);
            ch.Writer.TryWrite(new CommandWork { Req = req, Tcs = tcs });

            _workers.GetOrAdd(key, _ => Task.Run(() => WorkerLoop(key, ch)));

            return tcs.Task;
        }

        private async Task WorkerLoop((string BusId, int FlowerId) key, Channel<CommandWork> ch)
        {
            var protocol = _buses.GetProtocol(key.BusId);

            await foreach (var work in ch.Reader.ReadAllAsync())
            {
                // ---- HARD GATE: don't run on degraded/disconnected ----
                var fu = await _flowers.GetAsync(key.FlowerId);
                if (fu is null || fu.ConnectionStatus is ConnectionStatus.Degraded or ConnectionStatus.Disconnected)
                {
                    work.Tcs.TrySetResult(CommandOutcome.SkippedNotConnected);
                    continue;
                }

                if (work.Req.ShouldSkip != null && work.Req.ShouldSkip(fu))
                {
                    work.Tcs.TrySetResult(CommandOutcome.SkippedNoOp);
                    continue;
                }
                // -------------------------------------------------------

                var r = work.Req;
                var outcome = CommandOutcome.Timeout;

                try
                {
                    var corr = Guid.NewGuid();
                    var env = new ProtocolEnvelope(
                        corr, r.FlowerId, r.CommandId,
                        BuildPayload(r.Args), ProtocolMessageType.Command);

                    var ack = await protocol.SendAndWaitAckAsync(env, r.AckTimeout);
                    outcome = ack.Type == ProtocolMessageType.Ack ? CommandOutcome.Acked : CommandOutcome.Nacked;
                }
                catch (OperationCanceledException) { outcome = CommandOutcome.Timeout; }
                catch { outcome = CommandOutcome.Timeout; }

                // State transitions
                switch (outcome)
                {
                    case CommandOutcome.Acked:
                        if (r.StateOnAck != null) await _state.ApplyAsync(r.FlowerId, r.StateOnAck);
                        else await _state.TouchConnectionAsync(r.FlowerId, ConnectionStatus.Connected);
                        break;
                    case CommandOutcome.Nacked:
                        await _state.TouchConnectionAsync(r.FlowerId, ConnectionStatus.Degraded);
                        break;
                    case CommandOutcome.Timeout:
                        if (r.StateOnTimeout != null) await _state.ApplyAsync(r.FlowerId, r.StateOnTimeout);
                        else await _state.TouchConnectionAsync(r.FlowerId, ConnectionStatus.Degraded);
                        break;
                }

                work.Tcs.TrySetResult(outcome);
            }
        }

        private static ReadOnlyMemory<byte> BuildPayload(IReadOnlyDictionary<string, object?> args)
        {
            // map args to bytes; you already do this in each command or codec
            return ReadOnlyMemory<byte>.Empty;
        }

        public async ValueTask DisposeAsync()
        {
            foreach (var kv in _queues) kv.Value.Writer.TryComplete();
            await Task.WhenAll(_workers.Values);
        }
    }
}
