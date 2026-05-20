using CommonLibrary.Domain;

namespace ServicePortal.Application.Models
{
    public class UserContextModel
    {
        public UserData user { get; set; }
        public Tenant tenant { get; set; }
        public Venues venues { get; set; }
    }

    public class Tenant
    {
        public List<string> domains { get; set; }
        public string active_d { get; set; }
        public string expiry_d { get; set; }
        public string org_code { get; set; }
        public string org_name { get; set; }
        public string tenant_id { get; set; }
        public string cntrct_plan { get; set; }
        public bool svc { get; set; }
    }

    public class UserData
    {
        public string role { get; set; }
        public string email { get; set; }
        public string decr_email { get; set; }
        [Encrypt]
        public string last_name { get; set; }
        [Encrypt]
        public string first_name { get; set; }
        public List<object> last_access { get; set; }
        public string country_ip { get; set; }
    }

    public class Venues
    {
        public List<string> venue_id { get; set; }
    }
}
