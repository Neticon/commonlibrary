namespace CommonLibrary.Models.API
{
    public class CreateUserModel
    {
        public CreateUserData data { get; set; }
    }

    public class CreateUserData
    {
        public string first_name { get; set; }
        public string last_name { get; set; }
        public string email { get; set; }
        public string role { get; set; }
        public string phone_number { get; set; }
        public Guid? tenant_id { get; set; }
        public string create_bu {  get; set; }
    }
}
