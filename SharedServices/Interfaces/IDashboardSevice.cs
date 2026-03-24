using CommonLibrary.Models;
using Newtonsoft.Json.Linq;

namespace CommonLibrary.SharedServices.Interfaces
{
    public interface IDashboardSevice
    {
        Task<ServiceResponse> KeyMetrics(DashboardPayload data);
        Task<ServiceResponse> RecentBookingChanges(DashboardPayload data);
        Task<ServiceResponse> BelowAvarageRatings(DashboardPayload data);
        Task<ServiceResponse> BookingOutcome(DashboardPayload data);
        Task<ServiceResponse> BookFrequency(DashboardPayload data);
        Task<ServiceResponse> Usage(DashboardPayload data);

    }
}
