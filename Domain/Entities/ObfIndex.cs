namespace CommonLibrary.Domain.Entities
{
    public class ObfIndex : BaseEntity
    {
        public string org_code { get; set; }
        public Guid venue_id { get; set; }
        public Guid booking_id { get; set; }
        public string field { get; set; }
        public string obf_value { get; set; }
        public string date { get; set; }
    }
}