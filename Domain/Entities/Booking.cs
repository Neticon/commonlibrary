namespace CommonLibrary.Domain.Entities
{
    public class Booking : BaseEntity
    {
        public override string _schema => "web_app";
        public override string _table => "bookings";

        public Guid booking_id { get; set; }
        public Guid tenant_id { get; set; }
        public Guid venue_id { get; set; }
        public TimeSpan? create_dt { get; set; }
        public string create_bu { get; set; }
        public TimeSpan? modify_dt { get; set; }
        public string? modify_bu { get; set; }
        public TimeSpan? delete_dt { get; set; }
        public string? delete_bu { get; set; }
        public string? mods { get; set; }
        public string block_status { get; set; }
        public char type { get; set; }
        public string start_ts { get; set; }
        public string end_ts { get; set; }
        public int block_start { get; set; }
        public int block_end { get; set; }
        [Encrypt]
        public string? u_salutation { get; set; }
        [Encrypt]
        public string u_first { get; set; }
        [Encrypt]
        public string u_last { get; set; }
        [Encrypt]
        public string? u_phone { get; set; }
        [Encrypt]
        public string? u_phone_local { get; set; }
        [Encrypt]
        public string u_email { get; set; }
        [Encrypt]
        public string? u_message { get; set; }
        public string? u_reason { get; set; }
        public string review_result { get; set; }
        public string? review_description { get; set; }
        public int review_rank { get; set; }
        public int customer_rank { get; set; }
        public Guid evs_id { get; set; }
        public Guid pnvs_id { get; set; }
        public Guid ip_id { get; set; }
        public DateTime date { get; set; }
        public bool is_deleted { get; set; }
    }

    public enum BlockStatus
    {
        SCHEDULED,
        RESCHEDULED,
        CANCELED,
        CLOSED
    }
}
