namespace CommonLibrary.Domain.Entities
{
    public class Tenant : BaseEntity
    {
        public override string _table => "tenant";
        public override string _schema => "help_desk";
        public Guid tenant_id { get; set; }
        public TimeSpan create_dt { get; set; }
        public string create_bu { get; set; }
        public TimeSpan modify_dt { get; set; }
        public string modify_bu { get; set; }
        public TimeSpan delete_dt { get; set; }
        public string delete_bu { get; set; }
        public string mods { get; set; }
        public string org_name { get; set; }
        public string org_code { get; set; }
        public string cntrct_plan { get; set; }
        public string cntrct_origin { get; set; }
        public DateTime cntrct_d { get; set; }
        public DateTime active_d { get; set; }
        public DateTime expiry_d { get; set; }
        public string status { get; set; }
        public string domains { get; set; }
        public string web_pages { get; set; }
        public string intg_venue { get; set; }
        public string intg_video { get; set; }
        public string intg_keys { get; set; }
        public string library { get; set; }
        public bool configured { get; set; }
        public bool deployed { get; set; }
        public bool integrated { get; set; }
        public bool is_deleted { get; set; }
        public bool whitelabel { get; set; }
    }
}
