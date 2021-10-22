using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AlarmConnect.Models.Infrastructure
{
    internal class DataObjectWrapper
    {
        [JsonPropertyName("data")]
        public DataObject Data { get; set; }
        
        [JsonPropertyName("meta")]
        public Dictionary<string, object> Meta { get; set; }
    }
}
