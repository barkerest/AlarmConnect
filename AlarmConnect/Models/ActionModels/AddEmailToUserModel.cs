using System.Text.Json.Serialization;

namespace AlarmConnect.Models.ActionModels
{
    internal class AddEmailToUserModel
    {
        internal class UserLink
        {
            [JsonPropertyName("id")]
            public string Id   { get; set; }
            
            [JsonPropertyName("type")]
            public string Type { get; }

            public UserLink()
            {
                Type = ((IDataObjectFillable)new AlarmUser()).AcceptedApiType;
            }
        }
        
        [JsonPropertyName("address")]
        public string Address { get; set; }

        [JsonPropertyName("addressType")]
        public int AddressType { get; } = 2;

        [JsonPropertyName("canBeDeleted")]
        public bool CanBeDeleted { get; } = true;

        [JsonPropertyName("canBeEdited")]
        public bool CanBeEdited { get; } = true;

        [JsonPropertyName("canBeEnabled")]
        public bool CanBeEnabled { get; } = true;
        
        [JsonPropertyName("emailSendingFormat")]
        public int EmailSendingFormat { get; set; } = 1;    // 0 for plain text, 1 for html

        [JsonPropertyName("enabled")]
        public bool Enabled { get; } = true;

        [JsonPropertyName("invalid")]
        public bool Invalid { get; } = false;

        [JsonPropertyName("type")]
        public string Type { get; }
        
        [JsonPropertyName("user")]
        public UserLink User { get; }

        public AddEmailToUserModel()
        {
            Type = ((IDataObjectFillable)new AlarmEmailAddress()).AcceptedApiType;
            User = new UserLink();
        }
    }
}
