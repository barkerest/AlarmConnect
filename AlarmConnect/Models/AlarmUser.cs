using System;
using System.Collections.Generic;
using System.Linq;

namespace AlarmConnect.Models
{
    public class AlarmUser : IDataObjectFillable
    {
        public string Id { get; private set; }
        
        public string FirstName { get; private set; }
        
        public string LastName { get; private set; }
        
        public bool UseOnlyOneName { get; private set; }

        public string Name => UseOnlyOneName ? LastName : $"{FirstName} {LastName}".Trim();
        
        public bool IsPrimary { get; private set; }
        
        public int  UserType     { get; private set; }
        public bool IsEnterprise { get; private set; }
        public bool IsPaused     { get; private set; }

        public IReadOnlyList<AlarmEmailAddress> EmailAddresses  { get; private set; } = null;
        public IReadOnlyList<string>            EmailAddressIds { get; private set; }
        public IReadOnlyList<AlarmDeviceAccess> DeviceAccesses  { get; private set; } = null;
        public IReadOnlyList<string>            DeviceAccessIds { get; private set; }

        public string LoadedFromSystemId { get; private set; }
        
        string IDataObjectFillable.AcceptedApiType => "users/user";
        string IDataObjectFillable.ApiEndpoint     => "users/users";

        private ISession             _session;
        ISession IDataObjectFillable.Session => _session;

        
        void IDataObjectFillable.FillFromDataObject(IDataObject dataObject, ISession session)
        {
            if (dataObject is null) throw new ArgumentNullException(nameof(dataObject));
            if (dataObject.ApiType != ((IDataObjectFillable)this).AcceptedApiType) throw new ArgumentException("Data is not of the correct type.");
            _session = session ?? throw new ArgumentNullException(nameof(session));

            Id                 = dataObject.Id;
            FirstName          = dataObject.GetStringAttribute("firstName");
            LastName           = dataObject.GetStringAttribute("lastName");
            UseOnlyOneName     = dataObject.GetBooleanAttribute("useOnlyOneName");
            IsPrimary          = dataObject.GetBooleanAttribute("isPrimary");
            UserType           = dataObject.GetInt32Attribute("userType");
            IsEnterprise       = dataObject.GetBooleanAttribute("isEnterprise");
            IsPaused           = dataObject.GetBooleanAttribute("isPaused");
            EmailAddressIds    = dataObject.GetRelationships("emailAddresses").Select(x => x.Id).ToArray();
            DeviceAccessIds    = dataObject.GetRelationships("deviceAccess").Select(x => x.Id).ToArray();
            LoadedFromSystemId = session.GetSelectedSystem();
        }
    }
}
