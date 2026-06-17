using CommonLibrary.Domain.Entities;
using CommonLibrary.Helpers;
using CommonLibrary.Models;
using CommonLibrary.SharedServices;
using CommonLibrary.SharedServices.Interfaces;

namespace CommonLibrary.SharedServices.Services
{
    public class CurrentUserService : ICurrentUserService
    {
        public IContextSerivice _contextService;
        private CurrentUser? _cachedUser;

        public CurrentUser? CurrentUser => _cachedUser;
        private bool IsHelpDesk => AppConfig.AppType == AppType.Helpdesk;

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
            if (cacheContextUser == null || cacheContextUser.OrgCode != orgCode)
            {
                if (string.IsNullOrEmpty(orgCode))
                {
                    var conventusSecret = await _contextService.GetConventusSecret();
                    var decrEmail = AesEncryption.DecryptEcb(hashedMail, conventusSecret);
                    cacheContextUser = new CurrentUser { OrgCode = CommonConstants.Org_Code_Conventus, Email = hashedMail, Decr_Email = decrEmail, OrgSecret = conventusSecret, Role = UserRole.SUPER.ToString() };
                }
                else
                {
                    var tenantContext = await _contextService.GetTenantContext(orgCode);
                    if (tenantContext == null)
                        throw new Exception("Failed to get context for user!");
                    var emailSecret = IsHelpDesk ? await _contextService.GetConventusSecret() : tenantContext.TenantSecret;
                    var email = AesEncryption.DecryptEcb(hashedMail, emailSecret);
                    cacheContextUser = new CurrentUser { OrgCode = orgCode, Email = hashedMail, Decr_Email = email, OrgSecret = tenantContext.TenantSecret, TenantId = tenantContext.TenantId, ProductPlans = tenantContext.ProductPlan };
                }
            }

            _cachedUser = cacheContextUser;
            return _cachedUser;
        }
    }
}
