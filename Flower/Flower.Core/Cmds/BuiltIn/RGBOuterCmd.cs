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
    public sealed class RgbOuterCmd : IFlowerCommand
    {
        public string Id => "rgb.outer";
        public string DisplayName => "Set RGB (Outer Ring)";

        public IReadOnlyDictionary<string, object?>? args => _args;
        private static readonly IReadOnlyDictionary<string, object?> _args =
            new Dictionary<string, object?>
            {
                ["r"] = 255,
                ["g"] = 0,
                ["b"] = 100
            };

        private static readonly FlowerCategory[] _supported = { FlowerCategory.BigTulip, FlowerCategory.Any };
        public IReadOnlyCollection<FlowerCategory> SupportedCategories => _supported;

        public void ValidateArgs(FlowerCategory category, IReadOnlyDictionary<string, object?> args)
        {
            if (category != FlowerCategory.BigTulip)
                throw new NotSupportedException("RGBOUT is only supported for BigTulip.");

            ValidateRgbArgs(args);
        }

        public IReadOnlyList<byte[]> BuildPayload(
            int flowerId,
            FlowerCategory category,
            IReadOnlyDictionary<string, object?> args)
        {
            int r = (int)args["r"]!;
            int g = (int)args["g"]!;
            int b = (int)args["b"]!;
            var frame = $"{flowerId}/RGBOUT:{r},{g},{b}\n";
            return new[] { Encoding.ASCII.GetBytes(frame) };
        }

        public Func<FlowerUnit, FlowerUnit>? StateOnAck(
            FlowerCategory category,
            IReadOnlyDictionary<string, object?> args)
        {
            return f =>
            {
                f.ConnectionStatus = ConnectionStatus.Connected;
                return f;
            };
        }

        private static void ValidateRgbArgs(IReadOnlyDictionary<string, object?> args)
        {
            if (args is null) throw new ArgumentNullException(nameof(args));

            ValidateOne(args, "r");
            ValidateOne(args, "g");
            ValidateOne(args, "b");

            static void ValidateOne(IReadOnlyDictionary<string, object?> dict, string key)
            {
                if (!dict.TryGetValue(key, out var raw) || raw is null)
                    throw new ArgumentException($"Missing required argument: {key}");

                if (raw is not int v)
                    throw new ArgumentException($"Argument '{key}' must be an integer (0..255).");

                if (v < 0 || v > 255)
                    throw new ArgumentOutOfRangeException(key, $"'{key}' must be between 0 and 255 (inclusive).");
            }
        }
    }
}
