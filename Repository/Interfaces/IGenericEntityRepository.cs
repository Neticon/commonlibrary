using CommonLibrary.Domain.Entities;
using CommonLibrary.Domain.PSQL;
using Newtonsoft.Json.Linq;
using ServicePortal.Domain.PSQL;

namespace CommonLibrary.Repository.Interfaces
{
    public interface IGenericEntityRepository<T> where T : BaseEntity
    {
        Task<DoOperationResponse<T>> SaveEntity(T data);
        Task<DoOperationResponse<T>> UpdateEntity(Object model);
        Task<DoSelectOperationResponse<JObject>> GetData(Object model);
    }
}
