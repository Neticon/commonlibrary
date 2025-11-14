namespace CommonLibrary.Integrations.Model
{
    public class BookingViewModel
    {
        public string venue_id { get; set; } = "";
        public string tenant_id { get; set; } = "";   
        public int block_start { get; set; }
        public int block_end { get; set;}
        public string type { get; set; } = "";
        public string date { get; set; } = "";
        public string? service_id { get; set; } = null;
        public string u_reason { get; set; } = "";
    }
}
