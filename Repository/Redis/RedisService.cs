using StackExchange.Redis;

namespace CommonLibrary.Repository.Redis
{
    public class RedisService : IRedisService
    {
        private readonly IDatabase _database;

        public RedisService(IDatabase database)
        {
            _database = database;
        }

        public IDatabase GetDatabase()
        {
            return _database;
        }

        public async Task<RedisResult> SetJson(string key, string value)
        {
            var redisResult = await _database.ExecuteAsync("JSON.SET", key, ".", value);
            if (redisResult.IsNull || !redisResult.ToString().Equals("ok", StringComparison.OrdinalIgnoreCase))
                throw new Exception($"Failed to save key: {key} to redis - {redisResult}");
            return redisResult;
        }

        public async Task<bool> SetString(string key, string value, TimeSpan? expiry = null)
        {
            return await _database.StringSetAsync(key, value, expiry);
        }

        public async Task<string> GetString(string key)
        {
            return await _database.StringGetAsync(key);
        }

        public async Task<List<string>> MGet(List<string> keys)
        {
            var results = (RedisResult[]) await _database.ExecuteAsync("mget",keys.ToArray());
            if (results != null)
                return results.Select(x => x.ToString()).ToList();
            else
                return null;
        }

        public async Task<bool> Delete(string key)
        {
            return await _database.KeyDeleteAsync(key);
        }

        public async Task<RedisResult> ExecuteCommand(string command)
        {
            return await _database.ExecuteAsync(command);
        }

        public async Task<long> ExecuteHashIncrement(string key, string field, long inc = 1)
        {
            return await _database.HashIncrementAsync(key, field, inc);
        }
    }
}
