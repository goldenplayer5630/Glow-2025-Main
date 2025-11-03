// Flower.Core/Services/ShowPlayer.cs
using Flower.Core.Abstractions.Commands;
using Flower.Core.Abstractions.Services;
using Flower.Core.Enums;
using Flower.Core.Models;
using Flower.Core.Records;

namespace Flower.Core.Services
{
    public sealed class ShowPlayerService : IShowPlayerService
    {
        private readonly ICommandRegistry _commands;
        private readonly ICmdDispatcher _dispatcher; 
        private CancellationTokenSource? _cts;

        public ShowPlayerService(
            ICommandRegistry commands,
            ICmdDispatcher dispatcher)
        {
            _commands = commands;
            _dispatcher = dispatcher;
        }

        public Task PlayAsync(ShowProject project)
        {
            Stop();
            _cts = new CancellationTokenSource();

            // Build dispatchable events (logical → requests)
            var events = BuildDispatchableEvents(project);

            // Compute loop length in LONG ms
            long loopMs = project.Tracks
                .Select(t => (long)t.LoopMs)     // ensure long
                .DefaultIfEmpty(0L)
                .Max();

            if (project.Repeat && loopMs == 0L)
                loopMs = (events.LastOrDefault()?.AtMs ?? 0) + 1000L;

            // Run the timeline (single loop or repeating)
            return RunTimelineAsync(
                events,
                SafeInt(loopMs),                 // clamp to int for Task.Delay
                project.Repeat,
                _cts.Token);
        }

        private static int SafeInt(long value)
            => value > int.MaxValue ? int.MaxValue : (int)value;


        public void Stop()
        {
            _cts?.Cancel();
            _cts = null;
        }

        // ---- helpers ----

        private List<DispatchableEvent> BuildDispatchableEvents(ShowProject project)
        {
            var flowers = project.Flowers;
            var logical = project.Tracks
                .SelectMany(t => t.Events.Select(ev => (Track: t, Ev: ev)))
                .OrderBy(p => p.Ev.AtMs)
                .ToList();

            var list = new List<DispatchableEvent>(logical.Count);

            foreach (var x in logical)
            {
                var at = x.Ev.AtMs;
                var (flowerId, cmdId, args) = x.Ev.Event;

                var flower = flowers.FirstOrDefault(f => f.Id == flowerId)
                          ?? throw new InvalidOperationException($"Flower {flowerId} missing from project.");
                var cmd = _commands.GetById(cmdId);

                if (!cmd.SupportedCategories.Contains(flower.Category))
                    throw new InvalidOperationException($"Command {cmd.Id} not supported for {flower.Category}.");

                cmd.ValidateArgs(flower.Category, args);

                // Express *expected* state changes for dispatcher to apply on ACK/Timeout
                var onAck = cmd.StateOnAck(flower.Category, args); // e.g., set Open / brightness, mark Healthy
                var onTimeout = static (FlowerUnit f) =>
                {
                    f.ConnectionStatus = ConnectionStatus.Degraded;
                    return f;
                };


                var req = new CommandRequest(
                    FlowerId: flowerId,
                    CommandId: cmd.Id,
                    Args: args,
                    AckTimeout: TimeSpan.FromMilliseconds(400),
                    StateOnAck: onAck,
                    StateOnTimeout: onTimeout
                );

                list.Add(new DispatchableEvent(at, req));
            }

            return list;
        }

        private async Task RunTimelineAsync(
            IReadOnlyList<DispatchableEvent> events,
            int loopMs,
            bool repeat,
            CancellationToken ct)
        {
            do
            {
                var start = Environment.TickCount64;
                foreach (var e in events)
                {
                    var dueAt = start + e.AtMs;
                    // wait until it's time
                    while (!ct.IsCancellationRequested && Environment.TickCount64 < dueAt)
                    {
                        var remaining = (int)Math.Max(0, dueAt - Environment.TickCount64);
                        await Task.Delay(Math.Min(remaining, 25), ct); // coarse wait
                    }
                    ct.ThrowIfCancellationRequested();

                    // hand off to dispatcher → it will send, await ACK, and mutate state
                    // (You can choose fire-and-forget if you *don’t* want the timeline to block)
                    _ = _dispatcher.EnqueueAsync(e.Request, ct);
                }

                if (repeat)
                {
                    // ensure we honor loop length (in case last event < loopMs)
                    var loopEnd = start + loopMs;
                    while (!ct.IsCancellationRequested && Environment.TickCount64 < loopEnd)
                    {
                        var remaining = (int)Math.Max(0, loopEnd - Environment.TickCount64);
                        await Task.Delay(Math.Min(remaining, 25), ct);
                    }
                }
            }
            while (repeat && !ct.IsCancellationRequested);
        }
    }
}
