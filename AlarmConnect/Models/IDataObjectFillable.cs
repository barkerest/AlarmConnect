using AlarmConnect.Models.Infrastructure;

namespace AlarmConnect.Models
{
    public interface IDataObjectFillable
    {
        /// <summary>
        /// The ID for this data object.
        /// </summary>
        public string Id { get; }
        
        /// <summary>
        /// The API data type accepted by this object. 
        /// </summary>
        public string AcceptedApiType { get; }
        
        /// <summary>
        /// The API endpoint used by this object.
        /// </summary>
        public string ApiEndpoint { get; }

        /// <summary>
        /// The session this object was filled from.
        /// </summary>
        public ISession Session { get; }
        
        /// <summary>
        /// Fills the object contents from the specified data object and session handle.
        /// </summary>
        /// <param name="dataObject">The data object containing details.</param>
        /// <param name="session">The alarm.com session used to fill in additional details.</param>
        public void FillFromDataObject(IDataObject dataObject, ISession session);
    }
}
