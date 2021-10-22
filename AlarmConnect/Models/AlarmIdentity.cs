using System;
using System.Linq;

namespace AlarmConnect.Models
{
    public class AlarmIdentity : IDataObjectFillable
    {
        public string      Id                     { get; private set; }
        public bool        IsEnterprise           { get; private set; }
        public bool        IsAccessControl        { get; private set; }
        public bool        IsCommercial           { get; private set; }
        public bool        IsManagedAccessAccount { get; private set; }
        public int         AccountType            { get; private set; }
        public AlarmDealer Dealer                 { get; private set; } = null;
        public string      DealerId               { get; private set; }
        public AlarmSystem SelectedSystem         { get; private set; } = null;
        public string      SelectedSystemId       { get; private set; }
        
        string IDataObjectFillable.AcceptedApiType => "identity";
        string IDataObjectFillable.ApiEndpoint     => "identities";

        private ISession             _session;
        ISession IDataObjectFillable.Session => _session;

        void IDataObjectFillable.FillFromDataObject(IDataObject dataObject, ISession session)
        {
            if (dataObject is null) throw new ArgumentNullException(nameof(dataObject));
            if (dataObject.ApiType != ((IDataObjectFillable)this).AcceptedApiType) throw new ArgumentException("Data is not of the correct type.");
            _session = session ?? throw new ArgumentNullException(nameof(session));

            Id                     = dataObject.Id;
            IsEnterprise           = dataObject.GetBooleanAttribute("isEnterprise");
            IsAccessControl        = dataObject.GetBooleanAttribute("isAccessControl");
            IsCommercial           = dataObject.GetBooleanAttribute("isCommercial");
            IsManagedAccessAccount = dataObject.GetBooleanAttribute("isManagedAccessAccount");
            AccountType            = dataObject.GetInt32Attribute("accountType");
            DealerId               = dataObject.GetRelationships("dealer").FirstOrDefault()?.Id;
            SelectedSystemId       = dataObject.GetRelationships("selectedSystem").FirstOrDefault()?.Id;
        }
    }
    
}
