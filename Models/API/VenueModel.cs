using CommonLibrary.Domain.Entities;
using Newtonsoft.Json.Linq;

namespace CommonLibrary.Models.API
{
    public class VenueModel
    {
        public VenueModelData data { get; set; }
        public VenueModelFilter? filters { get; set; }
    }

    public class VenueModelFilter
    {
        public string venue_id { get; set; }
    }

    public class VenueModelData
    {
        public string? name { get; set; }
        public string? street { get; set; }
        public string? street_number { get; set; }
        public string? street_addition { get; set; }
        public string? postal_code { get; set; }
        public string? province_code { get; set; }
        public string? province_name { get; set; }
        public string? country_code { get; set; }
        public string? currency_code { get; set; }
        public string? phone { get; set; }
        public string? email { get; set; }
        public double? longitude { get; set; }
        public double? latitude { get; set; }
        public string? time_zone { get; set; }
        public int? max_adv_days { get; set; }
        public int? min_lead_mins { get; set; }
        public bool? enabled { get; set; }
        public List<WorkHour>? work_hours { get; set; }
        public VenueNotifications? notifications { get; set; }
        public List<JObject>? users { get; set; } //to:do - check for specific structure
        public List<VenueExceptions>? exceptions { get; set; }
        public List<JObject>? reasons { get; set; } //to:do - check for specific structure
        public Links? links { get; set; }
        public string? description { get; set; }
    }
}
