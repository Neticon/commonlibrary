namespace CommonLibrary.Domain.Entities
{
    public class Block : BaseEntity
    {
        [IgnoreForSerialization]
        public override string _table => "blocks";
        [IgnoreForSerialization]
        public override string _schema => "service_portal";
        public Guid? block_id { get; set; }
        public Guid? venue_id { get; set; }
        public TimeSpan? create_dt { get; set; }
        [Encrypt]
        public string? create_bu { get; set; }
        public TimeSpan? modify_dt { get; set; }
        [Encrypt]
        public string? modify_bu { get; set; }
        public TimeSpan? delete_dt { get; set; }
        [Encrypt]
        public string? delete_bu { get; set; }
        public string? mods { get; set; }
        public bool? enabled { get; set; }
        public int? day { get; set; }
        public string? type { get; set; }
        public int? slots { get; set; }
        public int? block_start { get; set; }
        public int? block_end { get; set; }
        public bool? is_deleted { get; set; }
        public string? service { get; set; }
    }
}
