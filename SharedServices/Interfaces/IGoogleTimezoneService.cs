using CommonLibrary.Models;
using CommonLibrary.Models.API;

namespace CommonLibrary.SharedServices.Interfaces
{
    public interface IGoogleTimezoneService
    {
        Task<ServiceResponse> GetGoogleApiResponse(GoogleTimezoneApiPayload payload);
    }
}
