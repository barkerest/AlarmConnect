using System;
using System.Collections.Generic;
using System.Linq;

namespace AlarmConnect.Models
{
    public class AlarmSystem : IDataObjectFillable
    {
        public string Id { get; private set; }

        public string UnitId { get; private set; }

        public string Name { get; private set; }

        public string SystemGroupName { get; private set; }
        
        public IReadOnlyList<AlarmPartition> Partitions { get; private set; } = null;
        
        public IReadOnlyList<string>      PartitionIds { get; private set; }
        
        public IReadOnlyList<AlarmSensor> Sensors      { get; private set; } = null;
        
        public IReadOnlyList<string> SensorIds { get; private set; }

        string IDataObjectFillable.  AcceptedApiType => "systems/system";
        string IDataObjectFillable.  ApiEndpoint     => "systems/systems";

        private ISession             _session;
        ISession IDataObjectFillable.Session => _session;

        void IDataObjectFillable.FillFromDataObject(IDataObject dataObject, ISession session)
        {
            if (dataObject is null) throw new ArgumentNullException(nameof(dataObject));
            if (dataObject.ApiType != ((IDataObjectFillable)this).AcceptedApiType) throw new ArgumentException("Data is not of the correct type.");
            _session        = session ?? throw new ArgumentNullException(nameof(session));
            Id              = dataObject.Id;
            Name            = dataObject.GetStringAttribute("description");
            UnitId          = dataObject.GetStringAttribute("unitId");
            SystemGroupName = dataObject.GetStringAttribute("systemGroupName", defaultValue: "");
            PartitionIds    = dataObject.GetRelationships("partitions").Select(x => x.Id).ToArray();
            SensorIds       = dataObject.GetRelationships("sensors").Select(x => x.Id).ToArray();
        }
    }
}
