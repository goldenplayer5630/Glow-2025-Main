using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flower.Core.Cmds
{
    public static class ArgReader
    {
        public static T Get<T>(IReadOnlyDictionary<string, object?> args, string key, T? @default = default, bool required = true)
        {
            if (args.TryGetValue(key, out var raw) && raw is not null)
            {
                if (raw is T ok) return ok;
                // attempt number conversions (JSON numbers come as JsonElement/double/long sometimes)
                if (typeof(T) == typeof(int) && raw is IConvertible)
                    return (T)(object)Convert.ToInt32(raw);
                if (typeof(T) == typeof(double) && raw is IConvertible)
                    return (T)(object)Convert.ToDouble(raw);
                if (typeof(T) == typeof(bool) && raw is IConvertible)
                    return (T)(object)Convert.ToBoolean(raw);
                if (typeof(T) == typeof(string)) return (T)(object)raw.ToString()!;
            }

            if (!required) return @default!;
            throw new ArgumentException($"Missing or invalid arg '{key}'.");
        }
    }
}
