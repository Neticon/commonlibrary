namespace CommonLibrary.Integrations.Model
{
    public class BookingStatisticsPayload
    {
        public string tenant_id { get; set; }
        public List<string> venue_ids { get; set; }
        public string date { get; set; }
    }
}
