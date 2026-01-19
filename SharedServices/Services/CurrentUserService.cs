using CommonLibrary.Helpers;
using CommonLibrary.SharedServices.Interfaces;
using ServicePortal.Application.Interfaces;
using ServicePortal.Application.Models;

namespace ServicePortal.Application.Services
{
    public class CurrentUserService : ICurrentUserService
    {
        public ITenantContextSerivice _tenantContextService;
        private CurrentUser? _cachedUser;

        public CurrentUser? CurrentUser => _cachedUser;

        //add secretaManagerService 
        public CurrentUserService(ITenantContextSerivice tenantContextService)
        {
            _tenantContextService = tenantContextService;
        }
        public async Task<CurrentUser> GetAsync(string hashedMail, string orgCode)
        {
            if (_cachedUser != null)
                return _cachedUser;

            var tenantContext = await _tenantContextService.GetTenantContext(orgCode);
            var email = AesEncryption.Decrypt(hashedMail, tenantContext.TenantSecret);
            _cachedUser = new CurrentUser { OrgCode = orgCode, Email = email, HashedEmail = hashedMail, OrgSecret = tenantContext.TenantSecret, TenantId = tenantContext.TenantId };
            return _cachedUser;
        }
    }
}
