// InitCmd.cs
using System.Collections.Generic;
using System.Text;
using Flower.Core.Abstractions.Commands;
using Flower.Core.Enums;
using Flower.Core.Models;

namespace Flower.Core.Cmds.BuiltIn
{
    internal sealed class InitCmd : IFlowerCommand
    {
        public string Id => "init";
        public string DisplayName => "Initialize Node";

        // INIT has no configurable arguments
        public IReadOnlyDictionary<string, object?>? args => null;

        private static readonly FlowerCategory[] _supported =
        {
            FlowerCategory.SmallTulip,
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
            var frame = $"{flowerId}/INIT\n";
            return new[] { Encoding.ASCII.GetBytes(frame) };
        }

        public System.Func<FlowerUnit, FlowerUnit>? StateOnAck(
            FlowerCategory category,
            IReadOnlyDictionary<string, object?> args)
        {
            // On successful INIT we only assert the connection is healthy.
            return f =>
            {
                f.ConnectionStatus = ConnectionStatus.Connected;
                return f;
            };
        }
    }
}
