using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AlarmConnect.Models.Infrastructure
{
    internal static class InfrastructureExtensions
    {
        private class ErrorObject
        {
            [JsonPropertyName("status")]
            public string Status { get; set; }
            
            [JsonPropertyName("detail")]
            public string Detail { get; set; }
            
            [JsonPropertyName("code")]
            public int Code { get; set; }
        }

        private class ErrorCollection
        {
            [JsonPropertyName("errors")]
            public ErrorObject[] Errors { get; set; }
        }
        
        public static void CheckForMultiFactorRequirement(this HttpResponseMessage message)
        {
            try
            {
                // adc returns 409 when 2FA is required.
                if (message.StatusCode == HttpStatusCode.Conflict)
                {
                    var content = message.Content.ReadAsStringAsync().Result;
                    var data    = JsonSerializer.Deserialize<ErrorCollection>(content);

                    if (data?.Errors is null ||
                        data.Errors.Length < 1) return;

                    var e = data.Errors.FirstOrDefault(x => x.Code == 409);
                    if (e is null) return;

                    if (e.Detail.Equals("TwoFactorAuthenticationRequired"))
                    {
                        throw new MfaRequiredException();
                    }
                }
            }
            catch (JsonException)
            {
                // ignore since clearly the content wasn't meant for us to process.
            }
        }
    }
}
