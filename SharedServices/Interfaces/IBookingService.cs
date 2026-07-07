using CommonLibrary.Integrations.Model;
using CommonLibrary.Models;
using CommonLibrary.Models.API;

namespace CommonLibrary.SharedServices.Interfaces
{
    public interface IBookingService
    {
        Task<ServiceResponse> CreateBooking(BookingModelData data, string ip);
        Task<ServiceResponse> GetBooking(Guid bookingId);
        Task<ServiceResponse> UpdateBooking(BookingUpdateModel data);
        Task<ServiceResponse> RateBooking(BookingRateModel data);
        Task<OBFSearchResponse> SearchBooking(ObfSearchModel searchModel);
        Task<ServiceResponse> DeleteBooking(BookingUpdateModel data);

    }
}
