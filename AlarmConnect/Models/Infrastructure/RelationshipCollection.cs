using System.Text.Json.Serialization;
using AlarmConnect.Converters;

namespace AlarmConnect.Models.Infrastructure
{
    internal class RelationshipCollection
    {
        [JsonPropertyName("data")]
        [JsonConverter(typeof(RelationshipCollectionConverter))]
        public Relationship[] Data { get; set; }
    }
}
