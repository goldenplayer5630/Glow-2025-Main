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
    public sealed class MotorOpenCmd : IFlowerCommand
    {
        public string Id => "motor.open";
        public string DisplayName => "Open Motor";

        // No arguments needed for this command.
        public IReadOnlyDictionary<string, object?>? args => null;

        private static readonly FlowerCategory[] _supported =
        {
            FlowerCategory.SmallTulip,
            FlowerCategory.BigTulip,
        };
        public IReadOnlyCollection<FlowerCategory> SupportedCategories => _supported;

        public void ValidateArgs(FlowerCategory category, IReadOnlyDictionary<string, object?> args)
        {
            // No args to validate.
        }

        public IReadOnlyList<byte[]> BuildPayload(
            int flowerId,
            FlowerCategory category,
            IReadOnlyDictionary<string, object?> args)
        {
            var frame = $"{flowerId}/OPEN\n";
            return new[] { Encoding.ASCII.GetBytes(frame) };
        }

        public System.Func<FlowerUnit, FlowerUnit>? StateOnAck(
            FlowerCategory category,
            IReadOnlyDictionary<string, object?> args)
        {
            // On ACK: mark as Open + Healthy
            return f =>
            {
                f.FlowerStatus = FlowerStatus.Open;
                f.ConnectionStatus = ConnectionStatus.Connected;
                return f;
            };
        }
    }
}
