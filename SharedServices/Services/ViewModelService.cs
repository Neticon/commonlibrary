using CommonLibrary.Helpers;
using CommonLibrary.Integrations.Model;
using CommonLibrary.Models;
using CommonLibrary.Repository.Interfaces;
using CommonLibrary.SharedServices.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Npgsql;
using NpgsqlTypes;

namespace CommonLibrary.SharedServices.Services
{
    public class ViewModelService : IViewModelService
    {
        private readonly IGenericRepository<JObject> _repository;

        public ViewModelService(IGenericRepository<JObject> repository)
        {
            _repository = repository;
        }

        public async Task<ServiceResponse> GetBookVenueStatsViewModel(BookingStatisticsPayload payload)
        {
            var query = new NpgsqlCommand(PredefinedQueryPatterns.BOOKING_VENUES_STATISTICS);
            query.Parameters.AddWithValue("@payload", NpgsqlDbType.Jsonb, JsonConvert.SerializeObject(payload));

            var resp = await _repository.ExecuteStandardCommand(query);

            return new ServiceResponse { Result = resp };
        }

        public async Task<ServiceResponse> GetUsersTenantViewModel(string tenantId)
        {
            var fieldsForDecrypt = new List<string> { "first_name", "last_name", "phone_number", "create_bu", "modify_bu" };
            var query = new NpgsqlCommand(PredefinedQueryPatterns.USERS_TENANT_VIEW_MODEL);
            query.Parameters.AddWithValue("org_code", tenantId);

            var resp = await _repository.ExecuteStandardCommand(query);
            foreach (var row in resp.rows)
            {

                var users = row["users"];
                foreach (var user in users)
                {
                    ObjectEncryption.DecryptObject(user, "key", fieldsForDecrypt);
                    try
                    {
                        user["decr_email"] = AesEncryption.Decrypt(user["email"].ToString(), "key");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                    }
                }
            }

            return new ServiceResponse { Result = resp };
        }

        public async Task<ServiceResponse> GetVenuesTenantViewModel(string tenantId)
        {
            var query = new NpgsqlCommand(PredefinedQueryPatterns.VENUES_TENANT_VIEW_MODEL);
            query.Parameters.AddWithValue("tenant_id", tenantId);

            var resp = await _repository.ExecuteStandardCommand(query);

            return new ServiceResponse { Result = resp };
        }

        public async Task<ServiceResponse> GetBookList(JObject payload)
        {
            var query = new NpgsqlCommand(PredefinedQueryPatterns.BOOKING_LIST);
            query.Parameters.AddWithValue("@payload", NpgsqlDbType.Jsonb, JsonConvert.SerializeObject(payload));

            var resp = await _repository.ExecuteStandardCommand(query);

            return new ServiceResponse { Result = resp };
        }

        public async Task<ServiceResponse> GetBookingDetail(Guid bookingId) //done
        {
            var query = new NpgsqlCommand(PredefinedQueryPatterns.BOOKING_DETAIL);
            query.Parameters.AddWithValue("booking_id", NpgsqlDbType.Uuid, bookingId);

            var resp = await _repository.ExecuteStandardCommand(query);

            return new ServiceResponse { Result = resp };
        }
    }
}
