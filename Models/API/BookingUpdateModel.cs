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
        public string? block_status { get; set; }
        public string? start_ts { get; set; }
        public string? end_ts { get; set; }  
        public string? modify_bu {  get; set; }
        public DateTime? modify_dt { get; set; }
        public string? conference_upn { get; set; }
        public string? booking_uri { get; set; }
        public string? conference_id { get; set; }
        public string? review_result { get; set; }
        public string? review_description { get; set; }
        public int? review_rank { get; set; }
        public bool? is_deleted { get; set; }
        public string? delete_bu { get; set; }
        public DateTime? delete_dt { get; set; }

        public string? u_first {  get; set; }
        public string? u_last { get; set; }
        public string? u_message { get; set; }
        public string? u_phone { get; set; }
        public string? u_reason { get; set; }
        public string? u_salutation { get; set; }
        public Guid? pnvs_id { get; set; }
    }

    public class BookingUpdateFilters
    {
        public string? date { get; set; }
        public Guid booking_id { get; set; }
        public string? venue_id { get; set; }
        public string? tenant_id { get; set; }
    }

    public class WebApiUpdateData
    {
        public string? date { get; set; }
        public string? type { get; set; }
        public int? block_start { get; set; }
        public int? block_end { get; set; }
        public string? block_status { get; set; }
        public string? start_ts { get; set; }
        public string? end_ts { get; set; }
        public string modify_bu = "web_api";
    }
    public class WebApiBookingUpdateModel
    {
        public WebApiUpdateData data { get; set; }
        public BookingUpdateFilters filters { get; set; }
    }
}
