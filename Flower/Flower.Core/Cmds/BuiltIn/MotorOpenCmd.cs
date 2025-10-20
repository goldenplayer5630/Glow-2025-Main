using Flower.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flower.Core.Cmds.BuiltIn
{
    public class MotorOpenCmd : IFlowerCommand
    {
        public string Id => "motor.open";
        public string DisplayName => "Open Motor";
        public IReadOnlyCollection<FlowerCategory> SupportedCategories => new FlowerCategory[]
        {
            FlowerCategory.SmallTulip,
        };
        public void ValidateArgs(FlowerCategory category, IReadOnlyDictionary<string, object?> args)
        {
            // No arguments to validate for motor open
        }
        public IReadOnlyList<byte[]> BuildFrames(int flowerId, FlowerCategory category, IReadOnlyDictionary<string, object?> args)
        {
            string frame = $"{flowerId}/OPEN\n";
            byte[] frameBytes = Encoding.ASCII.GetBytes(frame);
            return new byte[][] { frameBytes };
        }
    }
}
