using Flower.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flower.Core.Cmds
{
    public interface IFlowerCommand
    {
        /// Unique id used in JSON, UI, etc. (e.g., "led.set", "motor.open")
        string Id { get; }

        /// Human name for UI ("Set LED", "Open Motor")
        string DisplayName { get; }

        /// Which categories support this command
        IReadOnlyCollection<FlowerCategory> SupportedCategories { get; }

        /// Validate arguments; throw with a clear message if invalid.
        void ValidateArgs(FlowerCategory category, IReadOnlyDictionary<string, object?> args);

        /// Build one or more serial frames to emit for this command on a given flower id.
        /// A frame is already ASCII-encoded (e.g., "3/LED:120\n").
        IReadOnlyList<byte[]> BuildFrames(int flowerId, FlowerCategory category, IReadOnlyDictionary<string, object?> args);
    }
}
