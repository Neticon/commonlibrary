using CommonLibrary.Integrations.Model;

namespace CommonLibrary.Repository.Interfaces
{
    public interface IObfIndexRepository
    {
        Task<string> InsertBulkIndexes(List<ObfIndexDBModel> models);
    }
}