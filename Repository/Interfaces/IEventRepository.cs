using CommonLibrary.Domain.Entities;

namespace CommonLibrary.Repository.Interfaces
{
    public interface IEventRepository : IGenericEntityRepository<Event>
    {
        Task InsertEvent(Event data);
        Task UpdateEventBody(Guid id, string origin, string body);
    }
}
