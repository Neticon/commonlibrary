namespace CommonLibrary.Domain.Entities
{
    public class OrgContacts : BaseEntity
    {
        [IgnoreForSerialization]
        public override string _table => "org_contacts";
        [IgnoreForSerialization]
        public override string _schema => "help_desk";
        public Guid? con_id { get; set; }
        public DateTime? create_dt { get; set; }
        public string? create_bu { get; set; }
        public DateTime? modify_dt { get; set; }
        public string? modify_bu { get; set; }
        public DateTime? delete_dt { get; set; }
        public string? delete_bu { get; set; }
        public string? mods { get; set; }
        public Guid? tenant_id { get; set; }
        [Encrypt]
        public string? first_name { get; set; }
        [Encrypt]
        public string? last_name { get; set; }
        [Encrypt]
        public string? email { get; set; }
        [Encrypt]
        public string? phone { get; set; }
        public Guid? evs_id { get; set; }
        public Guid? pnvs_id { get; set; }
        public bool? is_deleted { get; set; }
    }
}
