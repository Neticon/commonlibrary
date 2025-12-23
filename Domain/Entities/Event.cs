namespace CommonLibrary.Domain.Entities
{
    public class Event : BaseEntity
    {
        [IgnoreForSerialization]
        public override string _table => "events";
        [IgnoreForSerialization]
        public override string _schema => "help_desk";
        public Guid? event_id { get; set; }
        public TimeSpan? create_dt { get; set; }
        public TimeSpan? modify_dt { get; set; }
        public int? update_count { get; set; }
        public string? origin { get; set; }
        public string? body { get; set; }
        public string? reference_entity { get; set; }
        public string? message_type { get; set; }
        public Guid? reference_id { get; set; }
        public Guid? tenant_id { get; set; }
    }
}
