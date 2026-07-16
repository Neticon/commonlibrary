using CommonLibrary.Domain.Entities;
using CommonLibrary.Models;
using CommonLibrary.Models.API;

namespace CommonLibrary.SharedServices.Interfaces
{
    public interface ICurrentUserService
    {
        Task<CurrentUser> GetAsync(string hashedMail, string orgCode);
        CurrentUser? CurrentUser { get; }
        Task<TenantContextModel> GetTenantContext(string orgCode);
        Task<ProductPlans> GetProductPlan();
    }
}
