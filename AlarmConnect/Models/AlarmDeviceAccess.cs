using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AlarmConnect.Models
{
    public class AlarmDeviceAccess : IDataObjectFillable
    {
        public string Id { get; private set; }
        
        public string UserCode { get; private set; }
        
        public bool IsAccessPaused { get; private set; }
        
        public bool IsAllAccessUser { get; private set; }

        public IReadOnlyList<AlarmAccessPointCollectionSummary> AccessPointCollections   { get; private set; } = null;
        public IReadOnlyList<string>                            AccessPointCollectionIds { get; private set; }

        public IReadOnlyList<AlarmUser> Users   { get; private set; } = null;
        public IReadOnlyList<string>    UserIds { get; private set; }

        string IDataObjectFillable.  AcceptedApiType          => "users/access/device-access";
        string IDataObjectFillable.  ApiEndpoint              => "users/access/deviceAccesses"; 

        private ISession             _session;
        ISession IDataObjectFillable.Session => _session;

        void IDataObjectFillable.FillFromDataObject(IDataObject dataObject, ISession session)
        {
            if (dataObject is null) throw new ArgumentNullException(nameof(dataObject));
            if (dataObject.ApiType != ((IDataObjectFillable)this).AcceptedApiType) throw new ArgumentException("Data is not of the correct type.");
            _session = session ?? throw new ArgumentNullException(nameof(session));

            Id              = dataObject.Id;
            UserCode        = dataObject.GetStringAttribute("userCode");
            IsAccessPaused  = dataObject.GetBooleanAttribute("isAccessPaused");
            IsAllAccessUser = dataObject.GetBooleanAttribute("isAllAccessUser");

            AccessPointCollectionIds = dataObject.GetRelationships("accessPointCollectionsSummary").Select(x => x.Id).ToArray();
            UserIds                  = dataObject.GetRelationships("user").Select(x => x.Id).ToArray();
        }
    }
}
