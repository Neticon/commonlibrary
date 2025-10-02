using CommonLibrary.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonLibrary.Repository.Interfaces
{
    public interface IDeviceIntelRepository
    {
        Task CreateDeviceIntel(Guid id, string type, int origin, string apiJsonResponse, string hash);
        Task<DeviceIntel> GetDeviceIntel(string hash);
        Task<bool> UpdateDeviceIntel(Guid id, string type, string intel);
    }
}
