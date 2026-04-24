using CommonLibrary.Domain.Entities;
using CommonLibrary.Domain.PSQL;
using Newtonsoft.Json.Linq;
using ServicePortal.Domain.PSQL;

namespace CommonLibrary.Repository.Interfaces
{
    public interface IGenericEntityRepository<T>: IGenericRepository<T> where T : BaseEntity
    {
        Task<GraphAPIResponse<T>> SaveEntity(T data, string secret = "", bool returnError = false, List<string> includeNullList = null);
        Task<BulkGraphAPIResponse<T>> SaveEntityBulk(List<T> data, string secret = "", bool returnError = false, List<string> includeNullList = null);
        Task<GraphAPIResponse<T>> UpdateEntity(Object model, string secret = "", bool returnError = false, bool ignoreEncryption = false, List<string> includeNullList = null);
        Task<DoSelectOperationResponse<JObject>> GetData(Object model, string secret = "");
        Task<DoSelectOperationResponse<T>> GetDataTyped(Object model, string secret = "");
        Task<BulkGraphAPIResponse<T>> UpdateEntityBulk(List<Object> models, string secret = "", bool returnError = false, bool ignoreEncryption = false, List<string> includeNullList = null);
    }
}
