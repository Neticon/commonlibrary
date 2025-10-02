namespace CommonLibrary.Repository.Interfaces
{
    public interface IVenueRepository
    {
        Task<string> GetVenueTimezone(Guid id);
    }
}
