namespace ServicePortal.Application.Models
{
    public class CurrentUser
    {
        public string Email { get; set; }
        public string HashedEmail { get; set; }
        public string OrgCode { get; set; }
        public string OrgSecret { get; set; }
        public Guid TenantId { get; set; }
    }
}
