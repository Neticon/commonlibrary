using CommonLibrary.Helpers;
using CommonLibrary.Integrations;
using CommonLibrary.Repository.Interfaces;
using CommonLibrary.Repository.Redis;
using CommonLibrary.SharedServices;
using CommonLibrary.SharedServices.Interfaces;
using Npgsql;
using NpgsqlTypes;
using CommonLibrary.Models;

namespace CommonLibrary.SharedServices.Services
{
    public class AuthService : IAuthService
    {
        private readonly IValidationService _validationService;
        private readonly IGenericRepository<UserContextModel> _userContextRepository;
        private readonly IRedisService _redisService;
        private readonly IContextSerivice _contextService;
        private readonly bool IsHelpDesk = AppConfig.AppType == AppType.Helpdesk;

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
            var query = IsHelpDesk ? GenerateGetContextQuerySuper(hashedMail, org_code, lastAccess) : GenerateGetContextQuery(hashedMail, org_code, lastAccess);
            var userConextResponse = await _userContextRepository.ExecuteStandardCommand(query);
            if (userConextResponse.success && userConextResponse.rows.Count > 0)
            {
                var secret = await _redisService.GetString($"tenantSecret_{org_code}");
                var context = userConextResponse.rows.FirstOrDefault();
                context.user.decr_email = AesEncryption.DecryptEcb(context.user.email, IsHelpDesk ? await _contextService.GetConventusSecret() : secret);
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
                }else if(currentUser.OrgCode != org_code) //FOR HD => CHANGE SELECTED ORG_CODE
                {
                    currentUser.OrgCode = org_code;
                    currentUser.TenantId = new Guid(context.tenant.tenant_id);
                    currentUser.OrgSecret = secret;
                    currentUser.Venues = context.venues.venue_id;
                    _contextService.SetCurrentUserContext(currentUser.Email, currentUser);
                }
                var encryPaths = EncryptionMetadataHelper.GetEncryptedPropertyPaths(typeof(UserContextModel));
                ObjectEncryption.DecryptObject(context, IsHelpDesk ? await _contextService.GetConventusSecret() : secret, encryPaths.Item1, encryPaths.Item2);
                return new Tuple<UserContextModel, string, long>(context, currentUser.CSRF, currentUser.CSRF_Expiry);
            }
            else
                throw new Exception("Failed to get user context");
        }

        private NpgsqlCommand GenerateGetContextQuery(string email, string org_code, string lastAccess)
        {
            var query = new NpgsqlCommand(PredefinedQueryPatterns.GET_USER_CONTEXT);
            query.Parameters.AddWithValue("@p_email", NpgsqlDbType.Text, email);
            query.Parameters.AddWithValue("@p_org_code", NpgsqlDbType.Text, org_code);
            query.Parameters.AddWithValue("@p_last_access", NpgsqlDbType.Jsonb, lastAccess);
            return query;
        }

        private NpgsqlCommand GenerateGetContextQuerySuper(string email, string org_code, string lastAccess)
        {
            var query = new NpgsqlCommand(PredefinedQueryPatterns.GET_USER_CONTEXT_SUPER);
            query.Parameters.AddWithValue("@p_email", NpgsqlDbType.Text, email);
            query.Parameters.AddWithValue("@p_org_code", NpgsqlDbType.Text, org_code);
            query.Parameters.AddWithValue("@p_last_access", NpgsqlDbType.Jsonb, lastAccess);
            return query;
        }

        private string GenerateLastAccessObject(string ip, string city, string country)
        {
            var now = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
            return $"[{now},\"{ip}\",\"{city}\",\"{country}\"]";
        }
    }
}
