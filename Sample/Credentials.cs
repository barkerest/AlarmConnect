using AlarmConnect;
using AlarmConnect.Models;

namespace Sample
{
    public class Credentials : ILoginCredentials
    {
        public string UserName { get; set; }
        public string Password { get; set; }
    }
}
