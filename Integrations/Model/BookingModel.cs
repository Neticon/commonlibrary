namespace WebApp.API.Models
{
    public class BookingModel
    {
        public BookingModelData Data { get; set; }
    }
    public class BookingModelData
    {
        public string date { get; set; }
        public char type { get; set; }
        public string u_email { get; set; }
        public string u_phone { get; set; }
        public string u_salutation { get; set; }
        public string u_first { get; set; }
        public string u_last { get; set; }
        public Guid venue_id { get; set; }
        public Guid tenant_id { get; set; }
        public int block_start { get; set; }
        public int block_end { get; set; }
        public string u_message { get; set; }
        public string u_reason { get; set; }
        public string service_id { get; set; }
        public List<string>? val_fail { get; set; }
    }
}
