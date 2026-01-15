using CommonLibrary.Domain.Entities;
using CommonLibrary.Domain.PSQL;
using Newtonsoft.Json.Linq;
using ServicePortal.Domain.PSQL;

namespace CommonLibrary.Repository.Interfaces
{
    public interface IGenericEntityRepository<T> where T : BaseEntity
    {
        Task<GraphAPIResponse<T>> SaveEntity(T data, string secret = "", bool returnError = false);
        Task<GraphAPIResponse<T>> UpdateEntity(Object model, string secret = "", bool returnError = false);
        Task<DoSelectOperationResponse<JObject>> GetData(Object model, string secret = "");
    }
}
