using CommonLibrary.Domain.Entities;

namespace ServicePortal.Domain.Entities
{
    public class Venue : BaseEntity
    {
        public override string _table => "venues";
        public override string _schema => "service_portal";

        public string Name { get; set; }
        public string Email { get; set; }
        public Links links { get; set; }
        public string phone { get; set; }
        public string street { get; set; }
        public List<string> reasons { get; set; }
        public double latitude { get; set; }
        public string venue_id { get; set; }
        public double longitude { get; set; }
        public string tenant_id { get; set; }
        public string time_zone { get; set; }
        public List<WorkHour> work_hours { get; set; }
        public string postal_code { get; set; }
        public string country_code { get; set; }
        public int max_adv_days { get; set; }
        public object currency_code { get; set; }
        public int min_lead_mins { get; set; }
        public string province_code { get; set; }
        public string province_name { get; set; }
        public string street_number { get; set; }
        public object street_addition { get; set; }
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
