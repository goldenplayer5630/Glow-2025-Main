// LedSetCmd.cs
using System.Collections.Generic;
using System.Text;
using Flower.Core.Abstractions.Commands;
using Flower.Core.Enums;
using Flower.Core.Models;
using Flower.Core.Utilities;

namespace Flower.Core.Cmds.BuiltIn
{
    public sealed class LedSetCmd : IFlowerCommand
    {
        public string Id => "led.set";
        public string DisplayName => "Set LED";

        public IReadOnlyDictionary<string, object?>? args => _args;
        private static readonly IReadOnlyDictionary<string, object?> _args =
            new Dictionary<string, object?>
            {
                ["intensity"] = 15 // default; 0..255
            };

        private static readonly FlowerCategory[] _supported =
        {
            FlowerCategory.SmallTulip,
        };
        public IReadOnlyCollection<FlowerCategory> SupportedCategories => _supported;

        public void ValidateArgs(FlowerCategory category, IReadOnlyDictionary<string, object?> args)
        {
            if (args is null) throw new ArgumentNullException(nameof(args));
            if (!args.ContainsKey("intensity"))
                throw new ArgumentException("Missing required argument: intensity");

            if (!IntParser.TryGetInt(args["intensity"], out var intensity))
                throw new ArgumentException("Argument 'intensity' must be an integer (0..255).");

            if (intensity < 0 || intensity > 255)
                throw new ArgumentOutOfRangeException(nameof(intensity), "Intensity must be between 0 and 255 (inclusive).");
        }


        public IReadOnlyList<byte[]> BuildPayload(
            int flowerId,
            FlowerCategory category,
            IReadOnlyDictionary<string, object?> args)
        {
            int intensity = (int)args["intensity"]!;
            var frame = $"{flowerId}/LED:{intensity}\n";
            return new[] { Encoding.ASCII.GetBytes(frame) };
        }

        public System.Func<FlowerUnit, FlowerUnit>? StateOnAck(
            FlowerCategory category,
            IReadOnlyDictionary<string, object?> args)
        {
            int intensity = (int)args["intensity"]!;
            return f =>
            {
                f.CurrentBrightness = intensity;
                f.ConnectionStatus = ConnectionStatus.Connected;
                return f;
            };
        }
    }
}
