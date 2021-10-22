﻿using System;

namespace AlarmConnect.Models
{
    public class AlarmAvailableSystem : IDataObjectFillable
    {
        public string Id { get; private set; }
        
        public string Name { get; private set; }
        
        public bool IsSelected { get; private set; }
        
        string IDataObjectFillable.  AcceptedApiType => "systems/availableSystemItem";
        string IDataObjectFillable.  ApiEndpoint     => "systems/availableSystemItems";
        private ISession             _session;
        ISession IDataObjectFillable.Session => _session;
        void IDataObjectFillable.FillFromDataObject(IDataObject dataObject, ISession session)
        {
            if (dataObject is null) throw new ArgumentNullException(nameof(dataObject));
            if (dataObject.ApiType != ((IDataObjectFillable)this).AcceptedApiType) throw new ArgumentException("Data is not of the correct type.");
            _session = session ?? throw new ArgumentNullException(nameof(session));

            Id         = dataObject.Id;
            Name       = dataObject.GetStringAttribute("name");
            IsSelected = dataObject.GetBooleanAttribute("isSelected");
        }
    }
}
