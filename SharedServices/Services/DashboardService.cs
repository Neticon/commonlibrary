using CommonLibrary.Domain.Entities;
using CommonLibrary.Helpers;
using CommonLibrary.Models;
using CommonLibrary.Repository.Interfaces;
using CommonLibrary.SharedServices.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Npgsql;
using NpgsqlTypes;
using ServicePortal.Application.Interfaces;
using WebApp.API.Controllers.Helper;

namespace CommonLibrary.SharedServices.Services
{
    public class DashboardService : AppServiceBase, IDashboardSevice
    {
        private readonly IGenericRepository<JObject> _repository;
        private JsonSerializerSettings JsonIgnoreNullSettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore
        };

        public DashboardService(IGenericRepository<JObject> repository, ICurrentUserService currentUserService): base(currentUserService) 
        {
            _repository = repository;
        }

        public async Task<ServiceResponse> BelowAvarageRatings(DashboardPayload data)
        {
            var response = new ServiceResponse();
            if (!ValidatePayloadBasedOnRole(data))
            {
                response.StatusCode = 403;
                return response;
            }
            var query = new NpgsqlCommand(PredefinedQueryPatterns.GET_BELOW_AVERAGE_RATINGS);
            query.Parameters.AddWithValue("payload", NpgsqlDbType.Jsonb, JsonConvert.SerializeObject(data, JsonIgnoreNullSettings));
            
            response.Result = await _repository.ExecuteCommandTyped(query);
            return response;
        }

        public async Task<ServiceResponse> BookFrequency(DashboardPayload data)
        {
            var response = new ServiceResponse();
            if (!ValidatePayloadBasedOnRole(data))
            {
                response.StatusCode = 403;
                return response;
            }
            var query = new NpgsqlCommand(PredefinedQueryPatterns.GET_BOOKING_FREQUENCIES);
            query.Parameters.AddWithValue("payload", NpgsqlDbType.Jsonb, JsonConvert.SerializeObject(data, JsonIgnoreNullSettings));

            response.Result = await _repository.ExecuteCommandTyped(query);
            return response;
        }

        public async Task<ServiceResponse> BookingOutcome(DashboardPayload data)
        {
            var response = new ServiceResponse();
            if (!ValidatePayloadBasedOnRole(data))
            {
                response.StatusCode = 403;
                return response;
            }
            var query = new NpgsqlCommand(PredefinedQueryPatterns.GET_BOOKING_REVIEWS_RADAR);
            query.Parameters.AddWithValue("payload", NpgsqlDbType.Jsonb, JsonConvert.SerializeObject(data, JsonIgnoreNullSettings));

            response.Result = await _repository.ExecuteCommandTyped(query);
            return response;
        }


        public async Task<ServiceResponse> KeyMetrics(DashboardPayload data)
        {
            var response = new ServiceResponse();
            if (!ValidatePayloadBasedOnRole(data))
            {
                response.StatusCode = 403;
                return response;
            }
            var query = new NpgsqlCommand(PredefinedQueryPatterns.GET_KEY_METRICS);
            query.Parameters.AddWithValue("payload", NpgsqlDbType.Jsonb, JsonConvert.SerializeObject(data, JsonIgnoreNullSettings));

            response.Result = await _repository.ExecuteCommandTyped(query);
            return response;
        }

        public async Task<ServiceResponse> Usage(DashboardPayload data)
        {
            var response = new ServiceResponse();
            if (!ValidatePayloadBasedOnRole(data))
            {
                response.StatusCode = 403;
                return response;
            }
            var query = new NpgsqlCommand(PredefinedQueryPatterns.GET_USAGE_STATISTICS_MV);
            query.Parameters.AddWithValue("payload", NpgsqlDbType.Jsonb, JsonConvert.SerializeObject(data, JsonIgnoreNullSettings));

            response.Result = await _repository.ExecuteCommandTyped(query);
            return response;
        }

        public async Task<ServiceResponse> RecentBookingChanges(DashboardPayload data)
        {
            var response = new ServiceResponse();
            if (!ValidatePayloadBasedOnRole(data))
            {
                response.StatusCode = 403;
                return response;
            }

            var query = new NpgsqlCommand(PredefinedQueryPatterns.GET_RECENT_BOOKING_CHANGES);
            query.Parameters.AddWithValue("payload", NpgsqlDbType.Jsonb, JsonConvert.SerializeObject(data, JsonIgnoreNullSettings));
            var fieldsForDecrypt = new List<string> { "u_first", "u_last", "u_salutation" };
            var result = await _repository.ExecuteCommandTyped(query);
            if ((bool)result["success"] == false)
            {
                response.StatusCode = 400;
                response.Result = result["message"];
                return response;
            }
            foreach (var row in result["rows"])
            {
                ObjectEncryption.DecryptObject(row, CurrentUser.OrgSecret, fieldsForDecrypt, new List<string>());
            }

            response.Result = result;
            return response;
        }

        private bool ValidatePayloadBasedOnRole(DashboardPayload data)
        {
            var valid = true;
            var role = CommonHelperFunctions.GetRoleInt(CurrentUser.Role);
            if(data.venue_id == null && data.tenant_id == null) //must be HD user (SUPER)
            {
                if (role != (int)UserRole.SUPER)
                    valid = false;
            }else if(data.tenant_id != null) //must be ADMIN
            {
                if(role < (int)UserRole.ADMIN)
                    valid = false;
            }
            return valid;
        }
    }
}
