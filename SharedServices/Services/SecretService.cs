using CommonLibrary.Helpers;
using CommonLibrary.Repository.Redis;
using CommonLibrary.SharedServices.Interfaces;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;

namespace CommonLibrary.SharedServices.Services
{
    public class SecretService : ISecretService //NOT FINISHED ADD SECRET MANAGER IMPLEMENTATION WHEN IT IS READY
    {
        private readonly IAmazonSecretsManager _client;
        private readonly IRedisService _redisService;
        private readonly string REDIS_PREFIX = "tenantSecret_";

        public SecretService(IRedisService redisService, IAmazonSecretsManager amazonSecretsManager)
        {
            _redisService = redisService;
            _client = amazonSecretsManager;
        }
        public async Task<string> GetSecretAsync(string secretName)
        {
            var request = new GetSecretValueRequest
            {
                SecretId = secretName
            };

            var response = await _client.GetSecretValueAsync(request);

            return response.SecretString;
        }

        public async Task CreateSecretAsync(string secretName, string secretValue, string description = "")
        {
            var request = new CreateSecretRequest
            {
                Name = secretName,
                SecretString = secretValue,
                Description = description
            };

            await _client.CreateSecretAsync(request);
        }

        public async Task<string> GetEncryptionSecret(string orgCode)
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
