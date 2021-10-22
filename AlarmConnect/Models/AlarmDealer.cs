using System;

namespace AlarmConnect.Models
{
    public class AlarmDealer : IDataObjectFillable
    {
        public string Id { get; private set; }
        
        public string Name { get; private set; }
        
        public string SupportPhone { get; private set; }
        
        public string SupportEmail { get; private set; }
        
        public string SupportWebsite { get; private set; }

        string IDataObjectFillable.AcceptedApiType
            => "dealers/dealer";

        string IDataObjectFillable.  ApiEndpoint => "dealers/dealers";
        private ISession             _session;
        ISession IDataObjectFillable.Session => _session;
        void IDataObjectFillable.  FillFromDataObject(IDataObject dataObject, ISession session)
        {
            if (dataObject is null) throw new ArgumentNullException(nameof(dataObject));
            if (dataObject.ApiType != ((IDataObjectFillable)this).AcceptedApiType) throw new ArgumentException("Data is not of the correct type.");
            _session = session ?? throw new ArgumentNullException(nameof(session));

            Id             = dataObject.Id;
            Name           = dataObject.GetStringAttribute("name");
            SupportPhone   = dataObject.GetStringAttribute("supportPhone");
            SupportEmail   = dataObject.GetStringAttribute("supportEmail");
            SupportWebsite = dataObject.GetStringAttribute("externalSupportWebsite");
        }
    }
}
