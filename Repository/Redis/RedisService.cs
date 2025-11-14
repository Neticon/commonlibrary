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

        public async Task<HashEntry[]> GetHash(RedisKey key)
        {
            return await _database.HashGetAllAsync(key);
        }


        public async Task<List<string>> MGet(List<string> keys)
        {
            var results = (RedisResult[])await _database.ExecuteAsync("mget", keys.ToArray());
            if (results != null)
                return results.Select(x => x.ToString()).ToList();
            else
                return null;
        }

        public async Task<List<RedisKey>> GetKeysAsyncRedisArray(string pattern)
        {
            if (string.IsNullOrEmpty(pattern))
            {
                throw new Exception("Pattern is empty, cannot proceed with operation..");
            }
            var res = new List<RedisKey>();

            var mux = _database.Multiplexer;

            foreach (var endpoint in mux.GetEndPoints())
            {
                var server = mux.GetServer(endpoint);
                if (!server.IsConnected) continue;

                res.AddRange(server.Keys(pattern: pattern, pageSize: 1000));

            }

            return res;
        }

        public async Task<Dictionary<string, HashEntry[]>> GetHashValuesByPattern(string pattern)
        {
            var keys = await GetKeysAsyncRedisArray(pattern);
            var results = new Dictionary<string, HashEntry[]>();
            foreach (var key in keys)
            {
                results[key] = await GetHash(key);
            }
            return results;
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
