using CommonLibrary.Models;
using Newtonsoft.Json.Linq;

namespace CommonLibrary.SharedServices.Interfaces
{
    public interface IBlockService
    {
        Task<ServiceResponse> CreateBlocks(JToken payload);
        Task<ServiceResponse> DeleteBlocks(JToken payload);
        Task<ServiceResponse> UpdateBlocks(JToken payload);
        Task<ServiceResponse> GetBlocks(object payload);
        Task<string> GetAvaliableBlocks(string venueId, string date, string service);
    }
}
