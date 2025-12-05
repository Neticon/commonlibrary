using CommonLibrary.Domain.PSQL;

namespace CommonLibrary.Repository.Interfaces
{
    public interface IBlocksRepository
    {
        Task<BlockAvailabilityResponse> CheckBlocAvailability(int block_start, int block_end, string type, Guid venueId, string date, string service);
    }
}
