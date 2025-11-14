
using StackExchange.Redis;

namespace CommonLibrary.Repository.Redis
{
    public interface IRedisService
    {
        Task<bool> SetString(string key, string value, TimeSpan? expiry = null);
        Task<string> GetString(string key);
        Task<bool> Delete(string key);
        Task<RedisResult> ExecuteCommand(string command);
        Task<long> ExecuteHashIncrement(string key, string field, long inc = 1);
        Task<List<string>> MGet(List<string> keys);
        Task<Dictionary<string, HashEntry[]>> GetHashValuesByPattern(string pattern);
    }
}
