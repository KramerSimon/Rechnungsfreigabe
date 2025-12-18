using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RechnungsfreigabeAPI.Utilities
{
    // Json converter that accepts both numeric and string representations for enums
    public class FlexibleEnumConverter<TEnum> : JsonConverter<TEnum> where TEnum : struct, Enum
    {
        public override TEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                var str = reader.GetString();
                if (!string.IsNullOrWhiteSpace(str) && Enum.TryParse<TEnum>(str, true, out var parsed))
                {
                    return parsed;
                }
            }
            else if (reader.TokenType == JsonTokenType.Number)
            {
                if (reader.TryGetInt32(out var num) && Enum.IsDefined(typeof(TEnum), num))
                {
                    return (TEnum)Enum.ToObject(typeof(TEnum), num);
                }
            }

            throw new JsonException($"Invalid value for enum {typeof(TEnum).Name}");
        }

        public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options)
        {
            // Serialize as string for readability
            writer.WriteStringValue(value.ToString());
        }
    }
}
