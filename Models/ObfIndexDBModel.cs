namespace CommonLibrary.Integrations.Model
{
    public class ObfIndexDBModel
    {
        public string org_code { get; set; }
        public Guid venue_id { get; set; }
        public Guid booking_id { get; set; }
        public string field { get; set; }
        public string raw_value { get; set; }
        public string salt { get; set; }
        public string date { get; set; }
    }
}
