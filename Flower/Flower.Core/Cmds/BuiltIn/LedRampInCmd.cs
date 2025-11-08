using Flower.Core.Abstractions.Commands;
using Flower.Core.Enums;
using Flower.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flower.Core.Cmds.BuiltIn
{
    public sealed class LedRampInCmd : IFlowerCommand
    {
        public string Id => "led.ramp.in";
        public string DisplayName => "LED ramp inner";

        public IReadOnlyDictionary<string, object?>? args => _args;
        private static readonly IReadOnlyDictionary<string, object?> _args =
            new Dictionary<string, object?>
            {
                ["endIntensity"] = 50,  // 0..255
                ["durationMs"] = 3000  // >0
            };

        private static readonly FlowerCategory[] _supported =
        {
            FlowerCategory.BigTulip,
            FlowerCategory.Any
        };
        public IReadOnlyCollection<FlowerCategory> SupportedCategories => _supported;

        public void ValidateArgs(FlowerCategory category, IReadOnlyDictionary<string, object?> args)
        {
            if (!args.ContainsKey("endIntensity"))
                throw new ArgumentException("Missing required argument: endIntensity");
            if (args["endIntensity"] is not int endIntensity || endIntensity < 0 || endIntensity > 255)
                throw new ArgumentException("Argument 'endIntensity' must be an integer between 0 and 255.");

            if (!args.ContainsKey("durationMs"))
                throw new ArgumentException("Missing required argument: durationMs");
            if (args["durationMs"] is not int durationMs || durationMs <= 0)
                throw new ArgumentException("Argument 'durationMs' must be a positive integer.");
        }

        public IReadOnlyList<byte[]> BuildPayload(
            int flowerId,
            FlowerCategory category,
            IReadOnlyDictionary<string, object?> args)
        {
            int endIntensity = (int)args["endIntensity"]!;
            int durationMs = (int)args["durationMs"]!;
            var frame = $"{flowerId}/LEDRAMPIN:{endIntensity},{durationMs}\n";
            return new[] { Encoding.ASCII.GetBytes(frame) };
        }

        public System.Func<FlowerUnit, FlowerUnit>? StateOnAck(
            FlowerCategory category,
            IReadOnlyDictionary<string, object?> args)
        {
            int endIntensity = (int)args["endIntensity"]!;
            return f =>
            {
                f.CurrentBrightness = endIntensity;
                f.ConnectionStatus = ConnectionStatus.Connected;
                return f;
            };
        }
    }
}
