namespace CommonLibrary.Models.API
{
    public class BookingUpdateModel
    {
        public BookingUpdateData data { get; set; }
        public BookingUpdateFilters filters { get; set; }
    }

    public class BookingUpdateData
    {
        public string? date { get; set; }
        public string? type { get; set; }
        public int? block_start { get; set; }
        public int? block_end { get; set; }
        public string block_status { get; set; }
        public string start_ts { get; set; }
        public string end_ts { get; set; }  
        public string? modify_bu {  get; set; }
        public DateTime? modify_dt { get; set; }
        public string? conference_upn { get; set; }
    }

    public class BookingUpdateFilters
    {
        public string? date { get; set; }
        public Guid booking_id { get; set; }
        public string? venue_id { get; set; }
        public string? tenant_id { get; set; }
    }
}
