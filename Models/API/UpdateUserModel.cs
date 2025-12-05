namespace CommonLibrary.Models.API
{
    public class UpdateUserModel
    {
        public UpdateUserData data { get; set; }
        public UpdateUserFilters filters { get; set; }
    }

    public class UpdateUserData
    {
        public string first_name { get; set; }
        public string last_name { get; set; }
        public string role { get; set; }
        public string phone_number { get; set; }
    }

    public class UpdateUserFilters
    {
        public string email { get; set; }
        public Guid tenant_id { get; set; }
        public string idp_group { get; set; }
    }
}
