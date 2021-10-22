using System.Collections.Generic;
using System.Text.Json;

namespace AlarmConnect.Models
{
    /// <summary>
    /// The base interface for all data objects returned from the API.
    /// </summary>
    public interface IDataObject
    {
        /// <summary>
        /// The ID for this object.
        /// </summary>
        public string Id { get; }
        
        /// <summary>
        /// The type name of the API object.
        /// </summary>
        public string ApiType { get; }
        
        /// <summary>
        /// Get the names of the available attributes.
        /// </summary>
        /// <returns></returns>
        public IReadOnlyList<string> GetAttributeNames();
        
        /// <summary>
        /// Get an attribute value.
        /// </summary>
        /// <param name="name">The name of the attribute to retrieve.</param>
        /// <returns>Returns the attribute value or null if not set.</returns>
        public object GetAttribute(string name);
        
        /// <summary>
        /// Get the names of the available relationship collections.
        /// </summary>
        /// <returns></returns>
        public IReadOnlyList<string> GetRelationshipCollectionNames();

        /// <summary>
        /// Get a relationship collection.
        /// </summary>
        /// <param name="collectionName"></param>
        /// <returns>Will always return an array of relationships.</returns>
        public IRelationship[] GetRelationships(string collectionName);
    }
}
