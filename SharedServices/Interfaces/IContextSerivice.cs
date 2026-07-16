using CommonLibrary.Models.API;
using CommonLibrary.Models;
using CommonLibrary.Domain.Entities;

namespace CommonLibrary.SharedServices.Interfaces
{
    public interface IContextSerivice
    {
        public Task<TenantContextModel> GetTenantContext(string orgCode);
        public Task<CurrentUser?> GetCurrentUserContext(string email);
        public Task SetCurrentUserContext(string email, CurrentUser user);
        Task<string> GetConventusSecret();
        Task<string> GetOrgCodeByTenantId(Guid tenantId);
        Task<ProductPlans> GetProductPlan(string orgCode);
        Task InvalidateProductPlan(string orgCode);
    }
}
