using CommonLibrary.Models;
using CommonLibrary.Models.API;

namespace CommonLibrary.SharedServices.Interfaces
{
    public interface IBookingService
    {
        Task<ServiceResponse> CreateBooking(BookingModelData data, string ip);
        Task<ServiceResponse> GetBooking(Guid bookingId);
        Task<ServiceResponse> UpdateBooking(BookingUpdateModel data, string venue_id);
        Task<ServiceResponse> RateBooking(BookingRateModel data);
    }
}
