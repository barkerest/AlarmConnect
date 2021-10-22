using System;

namespace AlarmConnect.Models
{
    public class AlarmSensor : IDataObjectFillable
    {
        public AlarmSensor()
        {
            
        }

        public AlarmSensor(AlarmSensor s, AlarmSystem ss)
        {
            Id    = s.Id;
            Name  = ss.Name + ':' + s.Name;
            State = s.State;
        }
        
        public string Id                { get; private set; }
        public string Name              { get; private set; }
        public bool   HasState          { get; private set; }
        public int    State             { get; private set; }
        public string StateText         { get; private set; }
        public int    ManagedDeviceType { get; private set; }
        public string DeviceModelId     { get; private set; }
        public bool   IsMalfunctioning  { get; private set; }
        public bool   LowBattery        { get; private set; }
        public bool   CriticalBattery   { get; private set; }
        
        
        string IDataObjectFillable.  AcceptedApiType => "devices/sensor";
        string IDataObjectFillable.  ApiEndpoint     => "devices/sensors";
        private ISession             _session;
        ISession IDataObjectFillable.Session => _session;


        void IDataObjectFillable.FillFromDataObject(IDataObject dataObject, ISession session)
        {
            if (dataObject is null) throw new ArgumentNullException(nameof(dataObject));
            if (dataObject.ApiType != ((IDataObjectFillable)this).AcceptedApiType) throw new ArgumentException("Data is not of the correct type.");
            _session = session ?? throw new ArgumentNullException(nameof(session));

            Id                = dataObject.Id;
            Name              = dataObject.GetStringAttribute("description");
            StateText         = dataObject.GetStringAttribute("stateText");
            HasState          = dataObject.GetBooleanAttribute("hasState");
            State             = dataObject.GetInt32Attribute("state");
            ManagedDeviceType = dataObject.GetInt32Attribute("managedDeviceType");
            DeviceModelId     = dataObject.GetStringAttribute("deviceModelId");
            IsMalfunctioning  = dataObject.GetBooleanAttribute("isMalfunctioning");
            LowBattery        = dataObject.GetBooleanAttribute("lowBattery");
            CriticalBattery   = dataObject.GetBooleanAttribute("criticalBattery");
        }
    }
}
