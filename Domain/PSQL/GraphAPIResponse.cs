namespace CommonLibrary.Domain.PSQL
{
    public class GraphAPIResponse<T>
    {
        public List<T> rows { get; set; }
        public bool success { get; set; }
        public Guid request_id { get; set; }
        public List<object> affected_ids { get; set; }
        public string query { get; set; }
        public string stage { get; set; }
        public string message { get; set; }
        public string operation { get; set; }
        public List<string>? invalid_keys { get; set; }
        public int? count { get; set; }
        public string? entity {  get; set; } 
        public string? schema { get; set; }
        public int? per_page { get; set; }
        public int? total_pages { get; set; }
        public string? org_code { get; set; }
        public List<string> statuses { get; set; }
        public List<string> product_plans { get; set; }
    }

    public class BulkGraphAPIResponse<T>
    {
        public List<GraphAPIResponse<T>> results { get; set; }
    }

    public class TabulardataAPIResponse
    {
        public List<List<object>> rows { get; set; }
        public List<string> headings { get; set; }
        public bool success { get; set; }
        public Guid request_id { get; set; }
        public int? count { get; set; }
        public string? entity { get; set; }
        public string? schema { get; set; }
        public string? period { get; set; }
        public string? start_date { get; set; }
        public string? end_date { get; set; }
        public Guid? tenant_id { get; set; }
    }
}
