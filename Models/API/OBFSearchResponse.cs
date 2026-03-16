using CommonLibrary.Domain;
using Newtonsoft.Json.Linq;

namespace CommonLibrary.Models.API
{
    public class OBFSearchResponse
    {
        public bool success { get; set; }
        public string term { get; set; }
        public List<BookinSearchRow> rows { get; set; }
        public int count { get; set; }
        public int pages { get; set; }
        public int page {  get; set; }  
        public int page_size { get; set; }
        public string time_zone { get; set; }
        public string request_id { get; set; }
    }

    public class BookinSearchRow
    {
        public string type { get; set; }
        public DateTime end_ts { get; set; }
        [Encrypt]
        public string u_last { get; set; }
        [EncryptECB]
        public string u_email { get; set; }
        [Encrypt]
        public string u_first { get; set; }
        [Encrypt]
        public string u_phone { get; set; }
        public DateTime start_ts { get; set; }
        public string u_reason { get; set; }
        public int block_end { get; set; }
        public int dayOfWeek { get; set; }
        public string booking_id { get; set; }
        public MatchMeta match_meta { get; set; }
        public List<int> slot_range { get; set; }
        public int block_start { get; set; }
        public string block_status { get; set; }
        public int iso_week_num { get; set; }
        public string computed_status { get; set; }
    }

    public class Term
    {
        public string term { get; set; }
        public string field { get; set; }
    }

    public class MatchMeta
    {
        public List<Term> terms { get; set; }
        public int term_count { get; set; }
    }
}
