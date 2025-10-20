// Flower.Core/Services/ShowPlayer.cs
using Flower.Core.Cmds;          // your command abstractions
using Flower.Core.Models;
using Flower.Core.Services;

public sealed class ShowPlayer
{
    private readonly ShowScheduler _scheduler;
    private readonly ICommandRegistry _commands;
    private readonly IReadOnlyDictionary<int, FlowerUnit> _flowers;
    private CancellationTokenSource? _cts;

    public ShowPlayer(
        ShowScheduler scheduler,
        ICommandRegistry commands,
        IReadOnlyDictionary<int, FlowerUnit> flowers)
    {
        _scheduler = scheduler;
        _commands = commands;
        _flowers = flowers;
    }

    public Task PlayAsync(ShowProject project)
    {
        Stop();
        _cts = new CancellationTokenSource();

        // flatten + sort timed events
        var logical = project.Tracks
            .SelectMany(t => t.Events.Select(ev => (Track: t, Ev: ev)))
            .OrderBy(p => p.Ev.AtMs)
            .ToList();

        var emittable = new List<ShowEventResolved>(logical.Count);

        foreach (var x in logical)
        {
            var ev = x.Ev.Event;                // ShowEvent (FlowerId, CmdId, Args)
            var at = x.Ev.AtMs;

            if (!_flowers.TryGetValue(ev.flowerId, out var flower))
                throw new InvalidOperationException($"Unknown flower id {ev.flowerId}.");

            var cmd = _commands.GetById(ev.cmdId);

            if (!cmd.SupportedCategories.Contains(flower.Category))
                throw new InvalidOperationException(
                    $"Command '{cmd.Id}' not supported for {flower.Category}.");

            cmd.ValidateArgs(flower.Category, ev.Args);
            var frames = cmd.BuildFrames(ev.flowerId, flower.Category, ev.Args);

            emittable.Add(new ShowEventResolved(at, frames));
        }

        var loopMs = project.Tracks.Select(t => t.LoopMs).DefaultIfEmpty(0).Max();
        if (project.Repeat && loopMs == 0)
            loopMs = (int)(emittable.LastOrDefault()?.AtMs ?? 0) + 1000;

        return _scheduler.PlayLoopResolvedAsync(emittable, loopMs, _cts.Token);
    }

    public void Stop()
    {
        _cts?.Cancel();
        _cts = null;
    }
}
