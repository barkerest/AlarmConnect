using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using AlarmConnect.Converters;

namespace AlarmConnect.Models.Infrastructure
{
    internal class DataObject : IDataObject
    {
        /// <inheritdoc />
        [JsonPropertyName("id")]
        [JsonConverter(typeof(StringIdConverter))]
        public string Id { get; set; }
        
        /// <inheritdoc />
        [JsonPropertyName("type")]
        public string ApiType { get; set; }
        
        /// <summary>
        /// The attributes tied to this object.
        /// </summary>
        [JsonPropertyName("attributes")]
        public Dictionary<string,object> Attributes { get; set; }
        
        /// <summary>
        /// The relationships of this object.
        /// </summary>
        [JsonPropertyName("relationships")]
        public Dictionary<string,RelationshipCollection> Relationships { get; set; }

        /// <inheritdoc />
        public IReadOnlyList<string> GetAttributeNames()
            => Attributes.Keys.ToArray();

        /// <inheritdoc />
        public object GetAttribute(string name) => Attributes.ContainsKey(name) ? Attributes[name] : null;

        
        /// <inheritdoc />
        public IReadOnlyList<string> GetRelationshipCollectionNames()
            => Relationships.Keys.ToArray();

        /// <inheritdoc />
        public IRelationship[] GetRelationships(string collectionName)
            => (Relationships.TryGetValue(collectionName, out var collection) && collection?.Data != null) 
                   ? collection.Data.Cast<IRelationship>().ToArray() 
                   : Array.Empty<IRelationship>();
    }
}
