using CommonLibrary.Models;

namespace CommonLibrary.SharedServices.Interfaces
{
    public interface IEventService
    {
        Task<ServiceResponse> GetData(object payload);
    }
}
