using CommonLibrary.Models;

namespace CommonLibrary.SharedServices.Interfaces
{
    public interface ILogService
    {
        Task<ServiceResponse> WriteLog(LogModel payload);
    }
}
