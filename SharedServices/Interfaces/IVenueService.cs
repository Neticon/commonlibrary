using CommonLibrary.Models;
using CommonLibrary.Models.API;

namespace CommonLibrary.SharedServices.Interfaces
{
    public interface IVenueService
    {
        Task<ServiceResponse> GetVenues(object data);
        Task<ServiceResponse> DeleteVenue(DeleteVenueModel data);
        Task<ServiceResponse> CreateVenue(VenueModel data);
        Task<ServiceResponse> UpdateVenue(VenueModel data);
    }
}
