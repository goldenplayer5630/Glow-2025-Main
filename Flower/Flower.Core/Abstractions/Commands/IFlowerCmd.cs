using Flower.Core.Enums;
using Flower.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flower.Core.Abstractions.Commands
{
    public interface IFlowerCommand
    {
        /// Unique id used in JSON, UI, etc. (e.g., "led.set", "motor.open")
        string Id { get; }

        /// Human name for UI ("Set LED", "Open Motor")
        string DisplayName { get; }

        /// Available arguments for this command
        IReadOnlyDictionary<string, object?>? args { get; } // can be null/empty if no args

        /// Which categories support this command
        IReadOnlyCollection<FlowerCategory> SupportedCategories { get; }

        /// Validate arguments; throw with a clear message if invalid.
        void ValidateArgs(FlowerCategory category, IReadOnlyDictionary<string, object?> args);

        /// Build one or more serial frames to emit for this command on a given flower id.
        /// A frame is already ASCII-encoded (e.g., "3/LED:120\n").
        IReadOnlyList<byte[]> BuildPayload(int flowerId, FlowerCategory category, IReadOnlyDictionary<string, object?> args);

        Func<FlowerUnit, FlowerUnit>? StateOnAck(FlowerCategory category, IReadOnlyDictionary<string, object?> args);
    }
}
