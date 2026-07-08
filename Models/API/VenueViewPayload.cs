namespace CommonLibrary.Models.API
{
    public class VenueViewPayload
    {
        public Guid? p_tenant_id { get; set; }
        public string? p_org_code { get; set; }
        public Guid? p_venue_id { get; set; }
        public int? p_page { get; set; }
        public int? p_page_size { get; set; }
    }
}
