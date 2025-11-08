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
    public class MotorStopCmd : IFlowerCommand
    {
        public string Id => "motor.stop";
        public string DisplayName => "Stop Motor";
        public IReadOnlyDictionary<string, object?>? args => null;
        private static readonly FlowerCategory[] _supported =
        {
            FlowerCategory.BigTulip,
            FlowerCategory.Any
        };
        public IReadOnlyCollection<FlowerCategory> SupportedCategories => _supported;
        public void ValidateArgs(FlowerCategory category, IReadOnlyDictionary<string, object?> args)
        {
            // No arguments to validate
        }
        public IReadOnlyList<byte[]> BuildPayload(
            int flowerId,
            FlowerCategory category,
            IReadOnlyDictionary<string, object?> args)
        {
            var frame = $"{flowerId}/STOP\n";
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
    }
}
