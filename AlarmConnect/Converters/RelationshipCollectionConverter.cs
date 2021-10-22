using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using AlarmConnect.Models;
using AlarmConnect.Models.Infrastructure;

namespace AlarmConnect.Converters
{
    internal class RelationshipCollectionConverter : JsonConverter<Relationship[]>
    {
        public override Relationship[] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null) return null;
            if (reader.TokenType == JsonTokenType.StartObject)
            {
                return new[] {JsonSerializer.Deserialize<Relationship>(ref reader, options)};
            }
            if (reader.TokenType != JsonTokenType.StartArray) throw new JsonException("Expected array start.");
            var ret = new List<Relationship>();
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndArray)
                {
                    return ret.ToArray();
                }

                if (reader.TokenType == JsonTokenType.StartObject)
                {
                    ret.Add(JsonSerializer.Deserialize<Relationship>(ref reader, options));
                }
                else
                {
                    throw new JsonException("Expected relationship object.");
                }
            }
            
            throw new JsonException("Missing end of array.");
        }

        public override void Write(Utf8JsonWriter writer, Relationship[] value, JsonSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNullValue();
            }
            else if (value.Length == 0)
            {
                writer.WriteStartArray();
                writer.WriteEndArray();
            }
            else if (value.Length == 1)
            {
                JsonSerializer.Serialize(writer, value[0]);
            }
            else
            {
                writer.WriteStartArray();
                foreach (var item in value)
                {
                    JsonSerializer.Serialize(writer, item);
                }
                writer.WriteEndArray();
            }
        }
    }
}
