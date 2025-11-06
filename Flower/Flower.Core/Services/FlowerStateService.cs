using Flower.Core.Abstractions.Services;
using Flower.Core.Enums;
using Flower.Core.Models;

public sealed class FlowerStateService : IFlowerStateService
{
    private readonly IFlowerService _flowers;

    public FlowerStateService(IFlowerService flowers) => _flowers = flowers;

    public async Task TouchConnectionAsync(int id, ConnectionStatus status)
    {
        var f = await _flowers.GetAsync(id);
        if (f is null) return;

        // mutate only what you need
        f.ConnectionStatus = status;

        await _flowers.UpdateAsync(f);     // MUST update the same instance, not a new one
        await _flowers.SaveAsync();
    }

    public async Task ApplyAsync(int id, Func<FlowerUnit, FlowerUnit> mutate)
    {
        var f = await _flowers.GetAsync(id);
        if (f is null) return;

        // Ensure the mutator changes the existing instance (your lambdas already do `f => { f.ConnectionStatus = ...; return f; }`)
        mutate(f);

        await _flowers.UpdateAsync(f);
        await _flowers.SaveAsync();
    }
}
