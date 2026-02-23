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
    }

    public class BookingUpdateFilters
    {
        public string date { get; set; }
        public string booking_id { get; set; }
        public string? venue_id { get; set; }
    }
}
