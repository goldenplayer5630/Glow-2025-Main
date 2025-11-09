using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flower.Infrastructure.Converters
{
    /// <summary>
    /// Normalizes numbers coming from JSON so that integer literals
    /// become Int32 when they fit; only use Int64 if needed.
    /// Works for object, dictionaries, arrays, and nested objects.
    /// </summary>
    public sealed class PreferInt32ObjectConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) =>
            objectType == typeof(object) ||
            typeof(IDictionary<string, object?>).IsAssignableFrom(objectType);

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            var token = JToken.Load(reader);
            return Coerce(token);
        }

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            if (value is null) { writer.WriteNull(); return; }
            JToken.FromObject(value, serializer).WriteTo(writer);
        }

        private static object? Coerce(JToken t)
        {
            switch (t.Type)
            {
                case JTokenType.Integer:
                    var l = t.Value<long>();
                    if (l >= int.MinValue && l <= int.MaxValue) return (int)l;
                    return l;

                case JTokenType.Float: return t.Value<double>();
                case JTokenType.Boolean: return t.Value<bool>();
                case JTokenType.String: return t.Value<string>();
                case JTokenType.Null: return null;

                case JTokenType.Array:
                    return t.Select(Coerce).ToList();

                case JTokenType.Object:
                    {
                        var o = (JObject)t;
                        var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                        foreach (var p in o.Properties())
                            dict[p.Name] = Coerce(p.Value);
                        return dict;
                    }

                default:
                    return ((JValue)t).Value;
            }
        }
    }
}
