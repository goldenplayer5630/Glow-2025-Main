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
    public class PingCmd : IFlowerCommand
    {
        public string Id => "ping";
        public string DisplayName => "Ping Node";

        // Ping has no configurable arguments
        public IReadOnlyDictionary<string, object?>? args => null;

        private static readonly FlowerCategory[] _supported =
        {
            FlowerCategory.SmallTulip,
            FlowerCategory.BigTulip,
            FlowerCategory.Any
        };
        public IReadOnlyCollection<FlowerCategory> SupportedCategories => _supported;

        public void ValidateArgs(FlowerCategory category, IReadOnlyDictionary<string, object?> args)
        {
            // no args to validate
        }

        public IReadOnlyList<byte[]> BuildPayload(
            int flowerId,
            FlowerCategory category,
            IReadOnlyDictionary<string, object?> args)
        {
            var frame = $"{flowerId}/PING\n";
            return new[] { Encoding.ASCII.GetBytes(frame) };
        }

        public System.Func<FlowerUnit, FlowerUnit>? StateOnAck(
            FlowerCategory category,
            IReadOnlyDictionary<string, object?> args)
        {
            // On successful ping ACK, we only assert the connection is healthy.
            return f =>
            {
                f.ConnectionStatus = ConnectionStatus.Connected;
                return f;
            };
        }
    }
}
