using CommonLibrary.Models.API;
using CommonLibrary.Repository.Interfaces;
using CommonLibrary.Repository.Redis;
using CommonLibrary.SharedServices.Interfaces;
using Newtonsoft.Json;

namespace CommonLibrary.SharedServices.Services
{
    public class TenantContextService : ITenantContextSerivice
    {
        private readonly ITenantRepository _tenantRepo;
        private readonly ISecretService _secretService;
        private readonly IRedisService _redisService;
        private readonly string REDIS_KEY_PREFIX = "tenant_context:";

        public TenantContextService(ITenantRepository tenantRepo, ISecretService secretService, IRedisService redisService)
        {
            _tenantRepo = tenantRepo;
            _secretService = secretService;
            _redisService = redisService;
        }
        public async Task<TenantContextModel> GetTenantContext(string orgCode)
        {
            var key = $"{REDIS_KEY_PREFIX}{orgCode}";
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
    }
}
