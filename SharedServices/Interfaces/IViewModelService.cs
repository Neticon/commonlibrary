using CommonLibrary.Integrations.Model;
using CommonLibrary.Models;
using Newtonsoft.Json.Linq;

namespace CommonLibrary.SharedServices.Interfaces
{
    public interface IViewModelService
    {
        Task<ServiceResponse> GetUsersTenantViewModel(string tenantId);
        Task<ServiceResponse> GetVenuesTenantViewModel(string tenantId);
        Task<ServiceResponse> GetBookVenueStatsViewModel(BookingStatisticsPayload payload);
        Task<ServiceResponse> GetBookList(JObject payload);
        Task<ServiceResponse> GetBookingDetail(Guid bookingId);

    }
}
