using CommonLibrary.Domain.Entities;
using CommonLibrary.Integrations.Model;

namespace CommonLibrary.Repository.Interfaces
{
    public interface IObfIndexRepository : IGenericRepository<ObfIndex>
    {
        Task<string> InsertBulkIndexes(List<ObfIndexDBModel> models);
        Task<string> SearchBooking(ObfSearchModel data);

    }
}