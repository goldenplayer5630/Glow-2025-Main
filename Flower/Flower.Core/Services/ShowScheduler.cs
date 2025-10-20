// Flower.Core/Services/ShowScheduler.cs
using System.Diagnostics;
using Flower.Core.Abstractions;
using Flower.Core.Models;
using Flower.Core.Services;

public sealed class ShowScheduler
{
    private readonly ISerialPort _serial;
    private volatile bool _running;

    public ShowScheduler(ISerialPort serial) => _serial = serial;

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
                    await _serial.EnqueueAsync(frame, ct);
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


    // (keep your existing PlayLoopAsync(...) if you still use it elsewhere)
}
