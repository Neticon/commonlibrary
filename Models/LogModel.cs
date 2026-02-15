namespace CommonLibrary.Models
{
    public class LogModel
    {
        public LogModelData data { get; set; }
    }

    public class LogModelData
    {
        public string log_level { get; set; }
        public object input_payload { get; set; }
        public string operation_type { get; set; }
        public string entity { get; set; }
        public string message { get; set; }
        public string? client_addr { get; set; }
        public int? client_port { get; set; }
    }
}
