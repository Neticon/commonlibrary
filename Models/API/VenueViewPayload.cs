namespace CommonLibrary.Models.API
{
    public class VenueViewPayload
    {
        public Guid? tenant_id { get; set; }
        public string? org_code { get; set; }
        public Guid? venue_id { get; set; }
        public int? page { get; set; }
        public int? page_size { get; set; }
    }
}
