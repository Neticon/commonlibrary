namespace ServicePortal.Application.Models
{
    public class CurrentUser
    {
        public string Decr_Email { get; set; }
        public string Email { get; set; }
        public string OrgCode { get; set; }
        public string OrgSecret { get; set; }
        public Guid TenantId { get; set; }
        public string Role {  get; set; }   
    }
}
