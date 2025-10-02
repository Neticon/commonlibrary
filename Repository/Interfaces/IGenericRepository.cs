using CommonLibrary.Domain.PSQL;
using Newtonsoft.Json.Linq;
using ServicePortal.Domain.PSQL;

namespace CommonLibrary.Repository.Interfaces
{
    public interface IGenericRepository<T>
    {
        Task<DoOperationResponse<T>> ExecuteDoOperationsCommand(string query);
        Task<string> ExecuteCommandString(string query);
        Task ExecuteCommandVoid(string query);
        Task<DoSelectOperationResponse<T>> ExecuteDoSelectCommand(string query);
        Task<DoSelectOperationResponse<JObject>> ExecuteDoSelectCommandObject(string query);
        Task<T> ExecuteCommandTyped(string query);
    }
}
