namespace CommonLibrary.Domain.Entities
{
    public class AppCounters : BaseEntity
    {
        public override string _schema => "web_app";
        public override string _table => "app_counters";
        public Guid id { get; set; }
        public string date { get; set; }
        public Guid tenant_id { get; set; }
        public Guid? venue_id { get; set; }
        public int request { get; set; }
        public int key { get; set; }
        public int key_err_400 { get; set; }
        public int key_err_404 { get; set; }
        public int key_err_451 { get; set; }
        public int key_err_5xx { get; set; }
        public int renew { get; set; }
        public int renew_err_400 { get; set; }
        public int renew_err_451 { get; set; }
        public int renew_err_5xx { get; set; }
        public int venue_ok { get; set; }
        public int venue_err_400 { get; set; }
        public int venue_err_451 { get; set; }
        public int venue_err_5xx { get; set; }
        public int block_sel { get; set; }
        public int form_dis { get; set; }
        public int form_val_err { get; set; }
        public int book_ok { get; set; }
        public int book_err_400 { get; set; }
        public int book_err_451 { get; set; }
        public int book_err_5xx { get; set; }
        public int mod { get; set; }
        public int mod_sel { get; set; }
        public int mod_ok { get; set; }
        public int mod_canc { get; set; }
        public int mod_err_400 { get; set; }
        public int mod_err_451 { get; set; }
        public int mod_err_5xx { get; set; }
    }
}
