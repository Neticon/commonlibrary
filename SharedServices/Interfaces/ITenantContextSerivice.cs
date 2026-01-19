using CommonLibrary.Models.API;

namespace CommonLibrary.SharedServices.Interfaces
{
    public interface ITenantContextSerivice
    {
        public Task<TenantContextModel> GetTenantContext(string orgCode);
    }
}
