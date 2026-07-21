using System.Net;

namespace CommonLibrary.Domain.Entities
{
    public class LogsErr : BaseEntity
    {
        public override string _schema => "utility";
        public override string _table => "logs_errs";

        public long? log_id { get; set; }
        public DateTime? create_dt { get; set; }
        public string? log_level { get; set; }
        public string? event_type { get; set; }
        public Guid? request_id { get; set; }
        public string? operation_type { get; set; }
        public string? stage_name { get; set; }
        public string? error_code { get; set; }
        public string? sql_state { get; set; }
        public string? entity { get; set; }
        public string? message { get; set; }
        public object? input_payload { get; set; }
        public string? query_text { get; set; }
        public string? user_name { get; set; }
        public string? application_name { get; set; }
        public string? database_name { get; set; }
        public IPAddress? client_addr { get; set; }
        public int? client_port { get; set; }
    }

    public enum LogLevel
    {
        ERROR,
        INFO
    }

    public enum OperationType
    {
        GLOBAL_EXCEPTION,
        REMINDERS_JOB,
        SEND_GRID,
        MSFT_EVENT,
        MSFT_USER,
    }
}
