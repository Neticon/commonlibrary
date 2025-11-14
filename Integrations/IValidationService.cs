using CommonLibrary.Integrations.Model;
using CommonLibrary.Models;

namespace CommonLibrary.Integrations
{
    public interface IValidationService
    {
        Task<ValueTuple<int, string>> ValidateRequest(ValidateRequest data, bool apiCall = false);
        Task<RedisDeviceIntel> GetRedisDeviceIntel(string email, string phone, string ip);
        Task<GeoIPResponse> ValidateIp(string ip, int origin);
        Task<GeoIPResponse> GetIpData(string ip);
    }
}
