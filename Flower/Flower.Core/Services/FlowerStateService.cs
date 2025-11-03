using Flower.Core.Abstractions.Services;
using Flower.Core.Enums;
using Flower.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flower.Core.Services
{
    public sealed class FlowerStateService : IFlowerStateService
    {
        private readonly IFlowerService _flowerService;

        public FlowerStateService(IFlowerService flowerService)
        {
            _flowerService = flowerService;
        }

        public async Task ApplyAsync(int flowerId, Func<FlowerUnit, FlowerUnit> mutate)
        {
            var current = await _flowerService.GetAsync(flowerId);
            if (current is null) return;

            // If FlowerUnit is a class with settable props, you can mutate in-place under FlowerService lock.
            // To stay neutral, we clone -> update -> replace via UpdateAsync (triggers UI change reliably).
            var updated = CloneAndMutate(current, mutate);
            await _flowerService.UpdateAsync(updated);
        }

        public async Task TouchConnectionAsync(int flowerId, ConnectionStatus status)
        {
            await ApplyAsync(flowerId, f =>
            {
                f.ConnectionStatus = status;
                return f;
            });
        }

        private static FlowerUnit CloneAndMutate(FlowerUnit src, Func<FlowerUnit, FlowerUnit> mutate)
        {
            // Shallow copy; adjust if FlowerUnit has nested reference types that need deep copy
            var copy = new FlowerUnit
            {
                Id = src.Id,
                Category = src.Category,
                ConnectionStatus = src.ConnectionStatus,
                FlowerStatus = src.FlowerStatus,
                CurrentBrightness = src.CurrentBrightness,
                // ... copy other persisted fields the UI depends on
            };
            return mutate(copy);
        }
    }
}
