namespace CommonLibrary.Domain.PSQL
{
    public class DoSelectOperationResponse<T>
    {
        public List<T> rows { get; set; }
        public bool success { get; set; }
        public Guid request_id { get; set; }
        public int pages { get; set; }
        public int count { get; set; }
    }
}
