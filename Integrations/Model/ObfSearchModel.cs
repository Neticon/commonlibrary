namespace CommonLibrary.Integrations.Model
{
    public class ObfSearchModel
    {
        public string search { get; set; }
        public string? salt { get; set; }
        public string? org_code { get; set; }
        public string? venue_id { get; set; }
        public string? start_date { get; set; }
        public string? end_date { get; set; }
        public int? page { get; set; }
        public int? per_page { get; set; }
    }
}
