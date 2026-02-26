using CommonLibrary.Domain.Entities;
using CommonLibrary.Integrations.Model;
using Newtonsoft.Json.Linq;
using ServicePortal.Domain.PSQL;

namespace CommonLibrary.Repository.Interfaces
{
    public interface IBookingRepository : IGenericEntityRepository<Booking>
    {
        Task<GraphAPIResponse<Booking>> SaveBooking(Booking data);
        Task<BookingViewModel> GetBooking(Guid id);
        Task<GraphAPIResponse<Booking>> UpdateBooking(Object model);
        Task<string> GetBookingReason(Guid id);
        Task<object> RateBooking(Guid id, string date, int value);
        Task<GraphAPIResponse<JObject>> GetBookingUpdateData(Guid id);
    }
}
