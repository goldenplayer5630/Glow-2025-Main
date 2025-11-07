using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Flower.Core.Utilities
{
    public static class IntParser
    {
        public static bool TryGetInt(object? value, out int result)
        {
            switch (value)
            {
                case null:
                    result = 0; return false;

                case int i:
                    result = i; return true;

                case long l:
                    if (l < int.MinValue || l > int.MaxValue) { result = 0; return false; }
                    result = (int)l; return true;

                case double d:
                    // allow whole numbers like 15.0; reject fractional parts
                    if (Math.Abs(d - Math.Round(d)) > double.Epsilon) { result = 0; return false; }
                    var r = (long)Math.Round(d);
                    if (r < int.MinValue || r > int.MaxValue) { result = 0; return false; }
                    result = (int)r; return true;

                case string s:
                    return int.TryParse(s, out result);

                case JsonElement je:
                    // In case some code path still uses System.Text.Json
                    if (je.ValueKind == JsonValueKind.Number)
                    {
                        if (je.TryGetInt32(out var i32)) { result = i32; return true; }
                        if (je.TryGetInt64(out var i64))
                        {
                            if (i64 < int.MinValue || i64 > int.MaxValue) { result = 0; return false; }
                            result = (int)i64; return true;
                        }
                    }
                    if (je.ValueKind == JsonValueKind.String && int.TryParse(je.GetString(), out var fromStr))
                    { result = fromStr; return true; }
                    result = 0; return false;

                default:
                    // Last resort: IConvertible (e.g., boxed types)
                    if (value is IConvertible conv)
                    {
                        try { result = Convert.ToInt32(conv); return true; }
                        catch { /* fallthrough */ }
                    }
                    result = 0; return false;
            }
        }
    }
}
