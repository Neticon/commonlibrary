namespace ServicePortal.Domain.PSQL
{
    public class DoOperationResponse<T>
    {
        public List<T> rows { get; set; }
        public bool success { get; set; }
        public Guid request_id { get; set; }
        public List<object> affected_ids { get; set; }
        public string query { get; set; }
        public string stage { get; set; }
        public string message { get; set; }
        public string operation { get; set; }   
    }
}
