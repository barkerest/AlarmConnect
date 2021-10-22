using System;

namespace AlarmConnect.Models
{
    public class AlarmPartition : IDataObjectFillable
    {
        public AlarmPartition()
        {}

        public AlarmPartition(AlarmPartition p, AlarmSystem s)
        {
            Id   = p.Id;
            Name = s.Name + ':' + p.Name;
        }
        
        public string Id { get; private set; }
        
        public string Name { get; private set; }

        public int PartitionId { get; private set; }
        
        public bool HasState { get; private set; }
        
        public int State { get; private set; }

        public int    DesiredState { get; private set; }
        
        public int ManagedDeviceType { get; private set; }
        
        public string DeviceModelId { get; private set; }
        
        public bool IsMalfunctioning { get; private set; }

        public bool LowBattery { get; private set; }
        
        public bool CriticalBattery { get; private set; }
        
        string IDataObjectFillable.AcceptedApiType => "devices/partition";
        string IDataObjectFillable.ApiEndpoint     => "devices/partitions";

        private ISession             _session;
        ISession IDataObjectFillable.Session => _session;

        void IDataObjectFillable.FillFromDataObject(IDataObject dataObject, ISession session)
        {
            if (dataObject is null) throw new ArgumentNullException(nameof(dataObject));
            if (dataObject.ApiType != ((IDataObjectFillable)this).AcceptedApiType) throw new ArgumentException("Data is not of the correct type.");
            _session = session ?? throw new ArgumentNullException(nameof(session));

            Id                = dataObject.Id;
            Name              = dataObject.GetStringAttribute("description");
            PartitionId       = dataObject.GetInt32Attribute("partitionId");
            State             = dataObject.GetInt32Attribute("state");
            DesiredState      = dataObject.GetInt32Attribute("desiredState");
            ManagedDeviceType = dataObject.GetInt32Attribute("managedDeviceType");
            HasState          = dataObject.GetBooleanAttribute("hasState");
            IsMalfunctioning  = dataObject.GetBooleanAttribute("isMalfunctioning");
            DeviceModelId     = dataObject.GetStringAttribute("deviceModelId");
            LowBattery        = dataObject.GetBooleanAttribute("lowBattery");
            CriticalBattery   = dataObject.GetBooleanAttribute("criticalBattery");
        }
    }
}
