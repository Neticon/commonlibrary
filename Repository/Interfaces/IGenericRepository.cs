using CommonLibrary.Domain.PSQL;
using Newtonsoft.Json.Linq;
using Npgsql;
using ServicePortal.Domain.PSQL;

namespace CommonLibrary.Repository.Interfaces
{
    public interface IGenericRepository<T>
    {
        Task<GraphAPIResponse<T>> ExecuteStandardCommand(NpgsqlCommand query);
        Task<string> ExecuteCommandString(string query);
        Task ExecuteCommandVoid(string query);
        Task<DoSelectOperationResponse<T>> ExecuteDoSelectCommand(string query);
        Task<DoSelectOperationResponse<JObject>> ExecuteDoSelectCommandObject(string query);
        Task<T> ExecuteCommandTyped(string query);
    }
}
