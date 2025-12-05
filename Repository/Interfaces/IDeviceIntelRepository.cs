using CommonLibrary.Domain.Entities;

namespace CommonLibrary.Repository.Interfaces
{
    public interface IDeviceIntelRepository
    {
        Task CreateDeviceIntel(Guid id, string type, int origin, string apiJsonResponse, string hash);
        Task<DeviceIntel> GetDeviceIntel(string hash);
        Task<bool> UpdateDeviceIntel(Guid id, string type, string intel);
    }
}
