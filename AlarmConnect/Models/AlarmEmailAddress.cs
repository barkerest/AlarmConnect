using System;

namespace AlarmConnect.Models
{
    public class AlarmEmailAddress : IDataObjectFillable
    {
        public string Id            { get; private set; }
        public string Address       { get; private set; }
        public int    AddressType   { get; private set; }
        public int    SendingFormat { get; private set; }
        public bool   Enabled       { get; private set; }
        public bool   Invalid       { get; private set; }

        string IDataObjectFillable.AcceptedApiType => "users/email-address";

        string IDataObjectFillable.  ApiEndpoint => "users/emailAddresses";
        private ISession             _session;
        ISession IDataObjectFillable.Session => _session;

        void IDataObjectFillable.  FillFromDataObject(IDataObject dataObject, ISession session)
        {
            if (dataObject is null) throw new ArgumentNullException(nameof(dataObject));
            if (dataObject.ApiType != ((IDataObjectFillable)this).AcceptedApiType) throw new ArgumentException("Data is not of the correct type.");
            _session = session ?? throw new ArgumentNullException(nameof(session));

            Id            = dataObject.Id;
            Address       = dataObject.GetStringAttribute("address");
            AddressType   = dataObject.GetInt32Attribute("addressType");
            SendingFormat = dataObject.GetInt32Attribute("emailSendingFormat");
            Enabled       = dataObject.GetBooleanAttribute("enabled");
            Invalid       = dataObject.GetBooleanAttribute("invalid");
        }
    }
}
