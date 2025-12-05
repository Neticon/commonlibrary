using CommonLibrary.Models;

namespace CommonLibrary.SharedServices.Interfaces
{
    public interface IBlockService
    {
        Task<ServiceResponse> CreateBlocks(object payload);
        Task<ServiceResponse> DeleteBlocks(object payload);
        Task<ServiceResponse> UpdateBlocks(object payload);
        Task<ServiceResponse> GetBlocks(object payload);
    }
}
