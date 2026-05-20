using CommonLibrary.Helpers;
using CommonLibrary.Integrations;
using CommonLibrary.Repository.Interfaces;
using CommonLibrary.Repository.Redis;
using CommonLibrary.SharedServices.Interfaces;
using ServicePortal.Application.Interfaces;
using ServicePortal.Application.Models;

namespace ServicePortal.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IValidationService _validationService;
        private readonly IGenericRepository<UserContextModel> _userContextRepository;
        private readonly IRedisService _redisService;
        private readonly IContextSerivice _contextService;

        public AuthService(IValidationService validationService, IGenericRepository<UserContextModel> genericRepository, IRedisService redisService, IContextSerivice contextSerivice)
        {
            _validationService = validationService;
            _userContextRepository = genericRepository;
            _redisService = redisService;
            _contextService = contextSerivice;
        }

        public async Task<Tuple<UserContextModel, string, long>> GetUserContext(string org_code, string hashedMail, string ip)
        {
            var ipData = await _validationService.GetIpData(ip);
            var lastAccess = GenerateLastAccessObject(ip, ipData.city.name, ipData.country.isoCode);
            var query = GenerateGetContextQuery(hashedMail, org_code, lastAccess);
            var userConextResponse = await _userContextRepository.ExecuteStandardCommand(query);
            if (userConextResponse.success && userConextResponse.rows.Count > 0)
            {
                var secret = await _redisService.GetString($"tenantSecret_{org_code}");
                var context = userConextResponse.rows.FirstOrDefault();
                context.user.decr_email = AesEncryption.DecryptEcb(context.user.email, secret);
                context.user.country_ip = ipData.country.isoCode;
                var currentUser = await _contextService.GetCurrentUserContext(hashedMail);
                if (currentUser == null)
                {
                    var csfrToken = Guid.NewGuid().ToString();
                    var expiresAt = DateTimeOffset.UtcNow.AddMinutes(60).ToUnixTimeSeconds();
                    currentUser = new CurrentUser { Decr_Email = context.user.decr_email, Email = context.user.email, OrgCode = org_code, OrgSecret = secret, Role = context.user.role, TenantId = new Guid(context.tenant.tenant_id), CSRF = csfrToken, CSRF_Expiry = expiresAt, Venues = context.venues.venue_id };
                    _contextService.SetCurrentUserContext(currentUser.Email, currentUser);
                }
                else if (currentUser.Role != context.user.role)
                {
                    currentUser.Role = context.user.role;
                    _contextService.SetCurrentUserContext(currentUser.Email, currentUser);
                }
                var encryPaths = EncryptionMetadataHelper.GetEncryptedPropertyPaths(typeof(UserContextModel));
                ObjectEncryption.DecryptObject(context, secret, encryPaths.Item1, encryPaths.Item2);
                return new Tuple<UserContextModel, string, long>(context, currentUser.CSRF, currentUser.CSRF_Expiry);
            }
            else
                throw new Exception("Failed to get user context");
        }

        private string GenerateGetContextQuery(string email, string org_code, string lastAccess)
        {
            return $"select utility.get_user_context('{email}','{org_code}','{lastAccess}'::jsonb) as result;";
        }

        private string GenerateLastAccessObject(string ip, string city, string country)
        {
            var now = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
            return $"[{now},\"{ip}\",\"{city}\",\"{country}\"]";
        }
    }
}
