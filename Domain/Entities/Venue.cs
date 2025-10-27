using Newtonsoft.Json.Linq;

namespace CommonLibrary.Domain.Entities
{
    public class Venue : BaseEntity
    {
        public override string _table => "venues";
        public override string _schema => "service_portal";
        public Guid venue_id { get; set; }
        public Guid tenant_id { get; set; }
        public TimeSpan create_dt { get; set; }
        public string create_bu { get; set; }
        public TimeSpan modify_dt { get; set; }
        public string modify_bu { get; set; }
        public TimeSpan delete_dt { get; set; }
        public string delete_bu { get; set; }
        public string mods { get; set; }
        public string name { get; set; }
        public string street { get; set; }
        public string street_number { get; set; }
        public string street_addition { get; set; }
        public string postal_code { get; set; }
        public string city { get; set; }
        public string province_code { get; set; }
        public string province_name { get; set; }
        public string country_code { get; set; }
        public string currency_code { get; set; }
        public string phone { get; set; }
        public string email { get; set; }
        public double longitude { get; set; }
        public double latitude { get; set; }
        public string time_zone { get; set; }
        public int max_adv_days { get; set; }
        public int min_lead_mins { get; set; }
        public bool enabled { get; set; }
        public bool service_halt { get; set; }
        public object work_hours { get; set; }
        public object notifications { get; set; }
        public object  users { get; set; }
        public object exceptions { get; set; }
        public object reasons { get; set; }
        public object links { get; set; }
        public Guid evs_id { get; set; }
        public Guid pnvs_id { get; set; }
        public bool is_deleted { get; set; }
        public object description { get; set; }
        public object conifguration { get; set; }
    }

    public class WorkHour
    {
        public List<int> d { get; set; }
        public List<List<int>> h { get; set; }
    }

    public class Links
    {
        public string fb { get; set; }
        public string gm { get; set; }
        public string ig { get; set; }
        public string li { get; set; }
        public string pt { get; set; }
        public string tg { get; set; }
        public string wa { get; set; }
        public string ws { get; set; }
        public string yt { get; set; }
        public string logo { get; set; }
        public bool warn { get; set; }
        public string cover { get; set; }
    }
}
