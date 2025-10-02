namespace CommonLibrary.Domain.Entities
{
    public class DeviceIntel : BaseEntity
    {
        public override string _schema => "web_app";
        public override string _table => "device_intel";

        public Guid id { get; set; }
        public DateTime create_dt { get; set; }
        public string type { get; set; }
        public int origin { get; set; }
        public object intel { get; set; }
        public string hash { get; set; }
    }
}
