using CommonLibrary.Integrations.Model;
using CommonLibrary.Models;
using CommonLibrary.Models.API;
using Newtonsoft.Json.Linq;

namespace CommonLibrary.SharedServices.Interfaces
{
    public interface IViewModelService
    {
        Task<ServiceResponse> GetUsersTenantViewModel(Guid tenantId);
        Task<ServiceResponse> GetVenuesTenantViewModel(VenueViewPayload payload);
        Task<ServiceResponse> GetBookVenueStatsViewModel(BookingStatisticsPayload payload);
        Task<ServiceResponse> GetBookList(JObject payload);
        Task<ServiceResponse> GetBookingDetail(Guid bookingId);

    }
}
