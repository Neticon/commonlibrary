using CommonLibrary.Helpers;
using CommonLibrary.Models.API;
using CommonLibrary.Repository.Interfaces;
using CommonLibrary.Repository.Redis;
using CommonLibrary.SharedServices.Interfaces;
using Newtonsoft.Json;
using CommonLibrary.Models;
using CommonLibrary.Domain.Entities;

namespace CommonLibrary.SharedServices.Services
{
    public class ContextService : IContextSerivice
    {
        private readonly ITenantRepository _tenantRepo;
        private readonly ISecretService _secretService;
        private readonly IRedisService _redisService;
        private readonly IGenericEntityRepository<ProductPlans> _productPlanRepo;
        private readonly string REDIS_KEY_PREFIX_TENANT = "tenant_context:";
        private readonly string REDIS_KEY_PREFIX_USER = "user_context:";
        private readonly string REDIS_KEY_PREFIX_ORG_CODE = "tenant_org_code:";
        private readonly string REDIS_KEY_PREFIX_PRODUCT_PLAN = "tenant_product_plan:";

        public ContextService(ITenantRepository tenantRepo, ISecretService secretService, IRedisService redisService, IGenericEntityRepository<ProductPlans> productPlanRepo)
        {
            _tenantRepo = tenantRepo;
            _secretService = secretService;
            _redisService = redisService;
            _productPlanRepo = productPlanRepo;
        }

        public async Task<string> GetConventusSecret()
        {
            return await _secretService.GetEncryptionSecret(CommonConstants.Org_Code_Conventus);
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
            var secret = await _secretService.GetEncryptionSecret(orgCode);
            var tenant = await _tenantRepo.GetTenantContext(orgCode);
            if (tenant == null)
                throw new Exception($"GetCurrentTenantContext => Failed to find tenant id for org_code {orgCode}");
            var productPlanResp = await _productPlanRepo.GetDataTyped(new GraphApiPayload { data = new object(), filters = new ProductPlans { plan_id = tenant.cntrct_plan } });
            if (productPlanResp.success == false)
                throw new Exception($"GetCurrentTenantContext => Failed to find product plan for org_code {orgCode}, plan id {tenant.cntrct_plan}");
            var tenantContext = new TenantContextModel { TenantId = tenant.tenant_id.Value, TenantSecret = secret, ProductPlan = productPlanResp.rows.First() };
            _redisService.SetString(key, JsonConvert.SerializeObject(tenantContext));
            return tenantContext;
        }

        public async Task SetCurrentUserContext(string email, CurrentUser user)
        {
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            TimeSpan expiry = TimeSpan.FromSeconds(user.CSRF_Expiry - now);
            await _redisService.SetString($"{REDIS_KEY_PREFIX_USER}{email}", JsonConvert.SerializeObject(user), expiry);
        }

        public async Task<ProductPlans> GetProductPlan(string orgCode)
        {
            var key = $"{REDIS_KEY_PREFIX_PRODUCT_PLAN}{orgCode}";
            var cached = await _redisService.GetString(key);
            if (cached != null)
                return JsonConvert.DeserializeObject<ProductPlans>(cached);
            var tenant = await _tenantRepo.GetTenantContext(orgCode);
            if (tenant == null)
                throw new Exception($"GetProductPlan => Failed to find tenant id for org_code {orgCode}");
            var productPlanResp = await _productPlanRepo.GetDataTyped(new GraphApiPayload { data = new object(), filters = new ProductPlans { plan_id = tenant.cntrct_plan } });
            if (productPlanResp.success == false)
                throw new Exception($"GetProductPlan => Failed to find product plan for org_code {orgCode}, plan id {tenant.cntrct_plan}");
            var plan = productPlanResp.rows.First();
            await _redisService.SetString(key, JsonConvert.SerializeObject(plan));
            return plan;
        }

        public async Task InvalidateProductPlan(string orgCode)
        {
            await _redisService.Delete($"{REDIS_KEY_PREFIX_PRODUCT_PLAN}{orgCode}");
        }

        public async Task<string> GetOrgCodeByTenantId(Guid tenantId)
        {
            var key = $"{REDIS_KEY_PREFIX_ORG_CODE}{tenantId}";
            var cached = await _redisService.GetString(key);
            if (cached != null)
                return cached;

            var result = await _tenantRepo.GetOrgCodeAndName(tenantId);
            if (result == null)
                throw new Exception($"GetOrgCodeByTenantId => No tenant found for id {tenantId}");

            _redisService.SetString(key, result.Item1);
            return result.Item1;
        }
    }
}
