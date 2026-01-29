using CommonLibrary.Models.API;
using ServicePortal.Application.Models;

namespace CommonLibrary.SharedServices.Interfaces
{
    public interface IContextSerivice
    {
        public Task<TenantContextModel> GetTenantContext(string orgCode);
        public Task<CurrentUser?> GetCurrentUserContext(string email);
        public Task SetCurrentUserContext(string email, CurrentUser user);
    }
}
