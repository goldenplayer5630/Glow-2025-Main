using Flower.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flower.Core.Cmds.BuiltIn
{
    public class LedSetCmd : IFlowerCommand
    {
        public string Id => "led.set";
        public string DisplayName => "Set LED";
        public IReadOnlyCollection<FlowerCategory> SupportedCategories => new FlowerCategory[]
        {
            FlowerCategory.SmallTulip,
        };

        public void ValidateArgs(FlowerCategory category, IReadOnlyDictionary<string, object?> args)
        {
            if (!args.ContainsKey("intensity"))
            {
                throw new ArgumentException("Missing required argument: intensity");
            }
            if (args["intensity"] is not int intensity || intensity < 0 || intensity > 255)
            {
                throw new ArgumentException("Argument 'intensity' must be an integer between 0 and 255.");
            }
        }

        public IReadOnlyList<byte[]> BuildFrames(int flowerId, FlowerCategory category, IReadOnlyDictionary<string, object?> args)
        {
            int intensity = (int)args["intensity"]!;
            string frame = $"{flowerId}/LED:{intensity}\n";
            byte[] frameBytes = Encoding.ASCII.GetBytes(frame);
            return new byte[][] { frameBytes };
        }
    }
}
