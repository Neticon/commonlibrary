using CommonLibrary.Helpers;
using CommonLibrary.Repository.Redis;
using CommonLibrary.SharedServices.Interfaces;

namespace CommonLibrary.SharedServices.Services
{
    public class SecretServise : ISecretService //NOT FINISHED ADD SECRET MANAGER IMPLEMENTATION WHEN IT IS READY
    {
        private readonly IRedisService _redisService;
        private readonly string REDIS_PREFIX = "tenantSecret_";

        public SecretServise(IRedisService redisService)
        {
            _redisService = redisService;
        }
        public async Task<string> GetSecret(string orgCode)
        {
            //check Redis than manager
            //until HD tenant creation is not implemented if secret does not esist create new one and store for testing purpuses 
            var secret = await _redisService.GetString($"{REDIS_PREFIX}{orgCode}");

            //will be deleted later
            if(secret == null)
            {
                secret = AesEncryption.GenerateSecureString();
                await _redisService.SetString($"{REDIS_PREFIX}{orgCode}", secret);
            }
            return secret;
        }
    }
}
