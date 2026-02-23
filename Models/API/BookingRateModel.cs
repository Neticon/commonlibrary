using Newtonsoft.Json;

namespace CommonLibrary.Models.API
{
    public class BookingRateModel
    {
        public BookingRateData data { get; set; }
        public BookingRateFilters filters { get; set; }
    }

    public class BookingRateData
    {
        public int customer_rank { get; set; }
    }

    public class BookingRateFilters
    {
        //public int customer_rank { get; set; }
        public Guid booking_id { get; set; }
        // public BlockStatus block_status { get; set; }
        public string date { get; set; }
    }

    public class BlockStatus
    {
        public string[] values { get; set; }
        [JsonProperty("operator")]
        public string Operator { get; set; }
    }
}
