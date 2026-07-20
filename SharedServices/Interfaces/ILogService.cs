using CommonLibrary.Domain.Entities;
using CommonLibrary.Models;

namespace CommonLibrary.SharedServices.Interfaces
{
    public interface ILogService
    {
        Task<ServiceResponse> WriteLog(LogModel payload);
        Task WriteSystemLog(LogLevel logLevel, OperationType operationType, string message, string entity);
    }
}
