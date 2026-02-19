using CommonLibrary.Domain.Entities;

namespace CommonLibrary.Repository.Interfaces
{
    public interface IVenueRepository : IGenericEntityRepository<Venue>
    {
        Task<string> GetVenueTimezone(Guid id);
    }
}
