using CommonLibrary.Models.API;
using CommonLibrary.Models;

namespace CommonLibrary.SharedServices.Interfaces
{
    public interface IContextSerivice
    {
        public Task<TenantContextModel> GetTenantContext(string orgCode);
        public Task<CurrentUser?> GetCurrentUserContext(string email);
        public Task SetCurrentUserContext(string email, CurrentUser user);
        Task<string> GetConventusSecret();
    }
}
