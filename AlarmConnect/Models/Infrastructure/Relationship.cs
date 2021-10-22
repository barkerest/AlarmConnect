using System.Text.Json.Serialization;
using AlarmConnect.Converters;

namespace AlarmConnect.Models.Infrastructure
{
    internal class Relationship : IRelationship
    {
        [JsonPropertyName("id")]
        [JsonConverter(typeof(StringIdConverter))]
        public string Id { get; set; }
        
        [JsonPropertyName("type")]
        public string Type { get; set; }
    }
}
