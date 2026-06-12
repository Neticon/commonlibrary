using CommonLibrary.Domain.Entities;

namespace CommonLibrary.Models.API
{
    public class TenantContextModel
    {
        public Guid TenantId { get; set; }
        public string TenantSecret { get; set; }
        public ProductPlans ProductPlan { get; set; }
    }
}
