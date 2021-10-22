namespace AlarmConnect.Models
{
    public interface IRelationship
    {
        /// <summary>
        /// The ID value for the related item.
        /// </summary>
        public string Id { get; }
        
        /// <summary>
        /// The type of the related item.
        /// </summary>
        public string Type { get; }
    }
}
