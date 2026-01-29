using CommonLibrary.Helpers;
using CommonLibrary.SharedServices.Interfaces;
using ServicePortal.Application.Interfaces;
using ServicePortal.Application.Models;

namespace ServicePortal.Application.Services
{
    public class CurrentUserService : ICurrentUserService
    {
        public IContextSerivice _contextService;
        private CurrentUser? _cachedUser;

        public CurrentUser? CurrentUser => _cachedUser;

        //add secretaManagerService 
        public CurrentUserService(IContextSerivice tenantContextService)
        {
            _contextService = tenantContextService;
        }
        public async Task<CurrentUser> GetAsync(string hashedMail, string orgCode)
        {
            if (_cachedUser != null)
                return _cachedUser;

            var cacheContextUser = await _contextService.GetCurrentUserContext(hashedMail);
            if (cacheContextUser == null) //no context, fallback to create context
            { 
                var tenantContext = await _contextService.GetTenantContext(orgCode);
                if(tenantContext == null)
                    throw new Exception("Failed to get context for user!");
                var email = AesEncryption.Decrypt(hashedMail, tenantContext.TenantSecret);
                cacheContextUser = new CurrentUser { OrgCode = orgCode, Email = hashedMail, Decr_Email = email, OrgSecret = tenantContext.TenantSecret, TenantId = tenantContext.TenantId };
            }

            _cachedUser = cacheContextUser;
            return _cachedUser;
        }
    }
}
