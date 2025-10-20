using Flower.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flower.Core.Cmds.BuiltIn
{
    public class MotorCloseCmd : IFlowerCommand
    {
        public string Id => "motor.close";
        public string DisplayName => "Close Motor";
        public IReadOnlyCollection<FlowerCategory> SupportedCategories => new FlowerCategory[]
        {
            FlowerCategory.SmallTulip,
        };
        public void ValidateArgs(FlowerCategory category, IReadOnlyDictionary<string, object?> args)
        {
            // No arguments to validate for motor close
        }
        public IReadOnlyList<byte[]> BuildFrames(int flowerId, FlowerCategory category, IReadOnlyDictionary<string, object?> args)
        {
            string frame = $"{flowerId}/CLOSE\n";
            byte[] frameBytes = Encoding.ASCII.GetBytes(frame);
            return new byte[][] { frameBytes };
        }
    }
}
