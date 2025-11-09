using Flower.Core.Abstractions.Commands;
using Flower.Core.Abstractions.Factories;
using Flower.Core.Abstractions.Services;
using Flower.Core.Models;
using Flower.Core.Records;
using Serilog;
using System.Diagnostics;

namespace Flower.Core.Services
{
    public sealed class ShowPlayerService : IShowPlayerService
    {
        private readonly ICommandRequestFactory _requestFactory;
        private readonly ICmdDispatcher _dispatcher;
        private readonly IFlowerService _flowerService;
        private CancellationTokenSource? _cts;
        private readonly IUiLogService? _uiLog;
        private readonly bool _dryRun;

        public ShowPlayerService(
            ICommandRequestFactory requestFactory,
            ICmdDispatcher dispatcher,
            IFlowerService flowerService,
            IUiLogService? uiLog = null)
        {
            _requestFactory = requestFactory;
            _dispatcher = dispatcher;
            _flowerService = flowerService;
            _uiLog = uiLog;
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
                _uiLog?.Info($"[SHOW] loop start  t0={start}  events={events.Count}  loopMs={loopMs}  repeat={repeat}");

                // Log the plan once (handy when pressing Play)
                foreach (var e in events)
                {
                    var req = e.Request;                 // <-- adjust if your type differs
                    var flowerId = req.FlowerId;         // <-- adjust if different (e.g., TargetId)
                    _uiLog?.Info($"[SHOW] plan  t+{e.AtMs,7} ms  ->  #{flowerId}  {req.CommandId}  args={ArgsToJson(req.Args)}");
                }

                foreach (var e in events)
                {
                    var dueAt = start + e.AtMs;

                    while (!ct.IsCancellationRequested && Environment.TickCount64 < dueAt)
                    {
                        var remaining = (int)Math.Max(0, dueAt - Environment.TickCount64);
                        await Task.Delay(Math.Min(remaining, 25), ct);
                    }
                    ct.ThrowIfCancellationRequested();

                    var now = Environment.TickCount64;
                    var drift = now - dueAt;                 // +late / -early
                    var offset = now - start;

                    var req = e.Request;                     // <-- adjust if needed
                    var flowerId = req.FlowerId;             // <-- adjust if needed

                    _uiLog?.Info($"[SHOW] send  t+{offset,7} ms  ({(drift >= 0 ? "+" : "")}{drift} ms)  ->  #{flowerId}  {req.CommandId}  args={ArgsToJson(req.Args)}");

                    try
                    {
                        // Prefer awaiting so logs reflect actual send order and catch errors here
                        await _dispatcher.EnqueueAsync(req, ct);
                    }
                    catch (Exception ex)
                    {
                        _uiLog?.Error($"[SHOW] dispatch failed  #{flowerId}  {req.CommandId}", ex);
                    }
                }

                if (repeat)
                {
                    var loopEnd = start + loopMs;
                    while (!ct.IsCancellationRequested && Environment.TickCount64 < loopEnd)
                    {
                        var remaining = (int)Math.Max(0, loopEnd - Environment.TickCount64);
                        await Task.Delay(Math.Min(remaining, 25), ct);
                    }
                    _uiLog?.Info($"[SHOW] loop end   t+{loopMs} ms");
                }
            }
            while (repeat && !ct.IsCancellationRequested);

            _uiLog?.Info(ct.IsCancellationRequested ? "[SHOW] cancelled" : "[SHOW] completed");
        }


        private static string ArgsToJson(IReadOnlyDictionary<string, object?>? args)
        {
            if (args is null || args.Count == 0) return "{}";
            // Stable ordering for tidy logs:
            var ordered = args.OrderBy(kv => kv.Key, StringComparer.Ordinal);
            return System.Text.Json.JsonSerializer.Serialize(ordered.ToDictionary(kv => kv.Key, kv => kv.Value));
        }

        private void LogSendPlanned(long atMs, string cmdId, int flowerId, IReadOnlyDictionary<string, object?>? args)
            => _uiLog?.Trace($"[SHOW] plan  t+{atMs,7} ms  ->  #{flowerId}  {cmdId}  args={ArgsToJson(args)}");

        private void LogSendDispatch(long nowOffsetMs, long driftMs, string cmdId, int flowerId, IReadOnlyDictionary<string, object?>? args)
        {
            var baseMsg = $"[SHOW] send  t+{nowOffsetMs,7} ms  ({(driftMs >= 0 ? "+" : "")}{driftMs} ms)  ->  #{flowerId}  {cmdId}  args={ArgsToJson(args)}";
            if (Math.Abs(driftMs) >= 50)
                _uiLog?.Warn(baseMsg);  // highlight when we’re 50ms or more off
            else
                _uiLog?.Info(baseMsg);
        }

    }
}
