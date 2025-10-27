using CommonLibrary.Domain.Entities;
using CommonLibrary.Domain.PSQL;
using Newtonsoft.Json.Linq;
using ServicePortal.Domain.PSQL;

namespace CommonLibrary.Repository.Interfaces
{
    public interface IGenericEntityRepository<T> where T : BaseEntity
    {
        Task<GraphAPIResponse<T>> SaveEntity(T data);
        Task<GraphAPIResponse<T>> UpdateEntity(Object model);
        Task<DoSelectOperationResponse<JObject>> GetData(Object model);
    }
}
