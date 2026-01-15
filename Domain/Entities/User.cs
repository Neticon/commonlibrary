namespace CommonLibrary.Domain.Entities
{
    public class User : BaseEntity
    {
        public override string _table => "users";
        public override string _schema => "service_portal";
        public override List<string> _encryptedFields => [""];
        public TimeSpan? create_dt { get; set; }
        [Encrypt]
        public string create_bu { get; set; }
        public TimeSpan? modify_dt { get; set; }
        [Encrypt]
        public string? modify_bu { get; set; }
        public TimeSpan? delete_dt { get; set; }
        [Encrypt]
        public string? delete_bu { get; set; }
        public string? mods { get; set; }
        public string email { get; set; }
        public string first_name { get; set; }
        public string last_name { get; set; }
        public string role { get; set; }
        public string? idp_group { get; set; }
        public string? idp_attribute { get; set; }
        public string phone_number { get; set; }
        public string? last_access { get; set; }
        public bool is_deleted { get; set; }
        public Guid? tenant_id { get; set; }
    }
}
