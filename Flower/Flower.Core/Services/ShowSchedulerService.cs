// ShowScheduler.cs (frame-based)
using Flower.Core.Abstractions.Commands;
using Flower.Core.Abstractions.Services;
using Flower.Core.Records;
using System.Diagnostics;

public sealed class ShowSchedulerService : IShowSchedulerService
{
    private readonly ITransport _transport;
    private volatile bool _running;

    public ShowSchedulerService(ITransport transport) => _transport = transport;

    public async Task PlayLoopResolvedAsync(
        IReadOnlyList<ShowEventResolved> events, int loopMs, CancellationToken ct)
    {
        _running = true;
        var sw = Stopwatch.StartNew();

        while (_running && !ct.IsCancellationRequested)
        {
            var baseMs = sw.ElapsedMilliseconds;

            foreach (var ev in events)
            {
                var due = baseMs + ev.AtMs;
                var wait = (int)Math.Max(0, due - sw.ElapsedMilliseconds);
                if (wait > 0) await Task.Delay(wait, ct);

                foreach (var frame in ev.Frames)
                    await _transport.WriteAsync(frame, ct);   // ← fix here
            }

            if (loopMs > 0)
            {
                var loopEnd = baseMs + loopMs;
                var pad = (int)Math.Max(0, loopEnd - sw.ElapsedMilliseconds);
                if (pad > 0) await Task.Delay(pad, ct);
            }
            else break;
        }
    }

    public void Stop() => _running = false;
}
