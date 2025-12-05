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
        public List<object>? work_hours { get; set; }
        public object? notifications { get; set; }
        public List<object>? users { get; set; }
        public object? exceptions { get; set; }
        public List<string>? reasons { get; set; }
        public object? links { get; set; }
        public string? description { get; set; }
    }
}
