using Flower.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flower.Core.Cmds.BuiltIn
{
    public class MotorOpenLedRampCmd : IFlowerCommand
    {
        public string Id => "motor.open.led.ramp";

        public string DisplayName => "Motor open, Led ramp";

        public IReadOnlyCollection<FlowerCategory> SupportedCategories => new FlowerCategory[]
        {
            FlowerCategory.SmallTulip,
        };

        public void ValidateArgs(FlowerCategory category, IReadOnlyDictionary<string, object?> args)
        {
            if (!args.ContainsKey("startIntensity"))
            {
                throw new ArgumentException("Missing required argument: startIntensity");
            }
            if (args["startIntensity"] is not int startIntensity || startIntensity < 0 || startIntensity > 255)
            {
                throw new ArgumentException("Argument 'startIntensity' must be an integer between 0 and 255.");
            }
            if (!args.ContainsKey("endIntensity"))
            {
                throw new ArgumentException("Missing required argument: endIntensity");
            }
            if (args["endIntensity"] is not int endIntensity || endIntensity < 0 || endIntensity > 255)
            {
                throw new ArgumentException("Argument 'endIntensity' must be an integer between 0 and 255.");
            }
            if (!args.ContainsKey("durationMs"))
            {
                throw new ArgumentException("Missing required argument: durationMs");
            }
            if (args["durationMs"] is not int durationMs || durationMs <= 0)
            {
                throw new ArgumentException("Argument 'durationMs' must be a positive integer.");
            }
        }

        public IReadOnlyList<byte[]> BuildFrames(int flowerId, FlowerCategory category, IReadOnlyDictionary<string, object?> args)
        {
            int startIntensity = (int)args["startIntensity"]!;
            int endIntensity = (int)args["endIntensity"]!;
            int durationMs = (int)args["durationMs"]!;
            string frame = $"{flowerId}/OPENLEDRAMP:{startIntensity},{endIntensity},{durationMs}\n";
            byte[] frameBytes = Encoding.ASCII.GetBytes(frame);
            return new byte[][] { frameBytes };
        }
    }
}
