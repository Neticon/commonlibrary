namespace CommonLibrary.Domain.Entities
{
    public class OrgLocations : BaseEntity
    {
        [IgnoreForSerialization]
        public override string _table => "org_locations";
        [IgnoreForSerialization]
        public override string _schema => "help_desk";
        public Guid? loc_id { get; set; }
        public DateTime? create_dt { get; set; }
        [EncryptECB]
        public string? create_bu { get; set; }
        public DateTime? modify_dt { get; set; }
        [EncryptECB]
        public string? modify_bu { get; set; }
        public DateTime? delete_dt { get; set; }
        [EncryptECB]
        public string? delete_bu { get; set; }
        public string? mods { get; set; }
        public Guid? tenant_id { get; set; }
        public bool? is_hq { get; set; }
        public bool? is_contract { get; set; }
        public string? street { get; set; }
        public string? street_add { get; set; }
        public string? city { get; set; }
        public string? postal_code { get; set; }
        public string? region_code { get; set; }
        public string? region_name { get; set; }
        public string? country_code { get; set; }
        public string? country_name { get; set; }
        public bool? is_deleted { get; set; }
    }
}
