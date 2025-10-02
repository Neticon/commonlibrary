using CommonLibrary.Integrations.Model;

namespace CommonLibrary.Integrations
{
    public interface IValidationService
    {
        Task<ValueTuple<int, string>> ValidateRequest(ValidateRequest data, bool apiCall = false);
        Task<RedisDeviceIntel> GetRedisDeviceIntel(string email, string phone, string ip);
        Task ValidateIp(string ip, int origin);
    }
}
