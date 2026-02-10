using CommonLibrary.Models.API;
using CommonLibrary.Repository.Interfaces;
using CommonLibrary.Repository.Redis;
using CommonLibrary.SharedServices.Interfaces;
using Newtonsoft.Json;
using ServicePortal.Application.Models;

namespace CommonLibrary.SharedServices.Services
{
    public class ContextService : IContextSerivice
    {
        private readonly ITenantRepository _tenantRepo;
        private readonly ISecretService _secretService;
        private readonly IRedisService _redisService;
        private readonly string REDIS_KEY_PREFIX_TENANT = "tenant_context:";
        private readonly string REDIS_KEY_PREFIX_USER = "user_context:";

        public ContextService(ITenantRepository tenantRepo, ISecretService secretService, IRedisService redisService)
        {
            _tenantRepo = tenantRepo;
            _secretService = secretService;
            _redisService = redisService;
        }

        public async Task<CurrentUser?> GetCurrentUserContext(string email)
        {
            var result = await _redisService.GetString($"{REDIS_KEY_PREFIX_USER}{email}");
            if (result == null)
                return null;
            return JsonConvert.DeserializeObject<CurrentUser>(result);
        }

        public async Task<TenantContextModel> GetTenantContext(string orgCode)
        {
            var key = $"{REDIS_KEY_PREFIX_TENANT}{orgCode}";
            var redisContext = await _redisService.GetString(key);
            if (redisContext != null)
                return JsonConvert.DeserializeObject<TenantContextModel>(redisContext);
            var secret = await _secretService.GetSecret(orgCode);
            var tenantId = await _tenantRepo.GetTenantId(orgCode);
            if (tenantId == null)
                throw new Exception($"GetCurrentTenantContext => Failed to find tenant id for org_code {orgCode}");
            var tenantContext = new TenantContextModel { TenantId = tenantId.Value, TenantSecret = secret };
            _redisService.SetString(key, JsonConvert.SerializeObject(tenantContext));
            return tenantContext;
        }

        public async Task SetCurrentUserContext(string email, CurrentUser user)
        {
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            TimeSpan expiry = TimeSpan.FromSeconds(user.CSRF_Expiry - now);
            await _redisService.SetString($"{REDIS_KEY_PREFIX_USER}{email}", JsonConvert.SerializeObject(user), expiry);
        }
    }
}
