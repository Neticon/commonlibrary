using CommonLibrary.Domain;

namespace CommonLibrary.Models.API
{
    public class DeleteUserModel
    {
        public DeleteUserData data { get; set; }
        public DeleteUserFilters filters { get; set; }
    }

    public class DeleteUserData
    {
        public bool is_deleted { get; set; }
        public string? email { get; set; }
        [Encrypt]
        public string? delete_bu { get; set; }
        public string? delete_dt { get; set; }
    }

    public class DeleteUserFilters
    {
        public string email { get; set; }
        public Guid tenant_id { get; set; }
        public string idp_group { get; set; }
    }
}
