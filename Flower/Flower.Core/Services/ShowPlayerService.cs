using Flower.Core.Abstractions.Commands;
using Flower.Core.Abstractions.Factories;
using Flower.Core.Abstractions.Services;
using Flower.Core.Models;
using Flower.Core.Records;
using System.Diagnostics;

namespace Flower.Core.Services
{
    public sealed class ShowPlayerService : IShowPlayerService
    {
        private readonly ICommandRequestFactory _requestFactory;
        private readonly ICmdDispatcher _dispatcher;
        private readonly IFlowerService _flowerService;   // NEW
        private CancellationTokenSource? _cts;

        public ShowPlayerService(
            ICommandRequestFactory requestFactory,
            ICmdDispatcher dispatcher,
            IFlowerService flowerService)                  // NEW
        {
            _requestFactory = requestFactory;
            _dispatcher = dispatcher;
            _flowerService = flowerService;
        }

        public Task PlayAsync(ShowProject project)
        {
            Stop();
            _cts = new CancellationTokenSource();

            var events = BuildDispatchableEvents(project);

            long loopMs = project.Tracks
                .Select(t => (long)t.LoopMs)
                .DefaultIfEmpty(0L)
                .Max();

            if (project.Repeat && loopMs == 0L)
                loopMs = (events.LastOrDefault()?.AtMs ?? 0) + 1000L;

            return RunTimelineAsync(
                events,
                SafeInt(loopMs),
                project.Repeat,
                _cts.Token);
        }

        public void Stop()
        {
            _cts?.Cancel();
            _cts = null;
        }

        private static int SafeInt(long value)
            => value > int.MaxValue ? int.MaxValue : (int)value;

        private List<DispatchableEvent> BuildDispatchableEvents(ShowProject project)
        {
            // pull live flowers from the runtime (detached from project)
            var flowers = _flowerService.Flowers; // assume IReadOnlyCollection<FlowerUnit>

            var logical = project.Tracks
                .SelectMany(t => t.Events.Select(ev => (Track: t, Ev: ev)))
                .OrderBy(p => p.Ev.AtMs)
                .ToList();

            var list = new List<DispatchableEvent>(logical.Count);

            foreach (var x in logical)
            {
                var at = x.Ev.AtMs;
                var (priority, cmdId, originalArgs) = x.Ev.Event;

                var flower = flowers.FirstOrDefault(f => f.Priority == priority);
                if (flower is null)
                {
                    Debug.WriteLine($"[ShowPlayer] Skipping event at {at}ms: no flower with Priority={priority}.");
                    continue; // detach-friendly: missing priority is fine
                }

                var req = _requestFactory.BuildFor(
                    flower,
                    cmdId,
                    originalArgs
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
                    while (!ct.IsCancellationRequested && Environment.TickCount64 < dueAt)
                    {
                        var remaining = (int)Math.Max(0, dueAt - Environment.TickCount64);
                        await Task.Delay(Math.Min(remaining, 25), ct);
                    }
                    ct.ThrowIfCancellationRequested();

                    _ = _dispatcher.EnqueueAsync(e.Request, ct);
                }

                if (repeat)
                {
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
