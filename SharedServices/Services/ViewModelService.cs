using CommonLibrary.Helpers;
using CommonLibrary.Integrations.Model;
using CommonLibrary.Models;
using CommonLibrary.Models.API;
using CommonLibrary.Repository.Interfaces;
using CommonLibrary.SharedServices.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Npgsql;
using NpgsqlTypes;

namespace CommonLibrary.SharedServices.Services
{
    public class ViewModelService : AppServiceBase, IViewModelService
    {
        private readonly IGenericRepository<JObject> _repository;

        public ViewModelService(IGenericRepository<JObject> repository, ICurrentUserService currentUserService) : base(currentUserService)
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

        public async Task<ServiceResponse> GetUsersTenantViewModel(Guid tenantId)
        {
            var fieldsForDecrypt = new List<string> { "first_name", "last_name", "phone_number" };
            var fieldsForDecryptECB = new List<string> { "create_bu", "modify_bu" };
            var query = new NpgsqlCommand(PredefinedQueryPatterns.USERS_TENANT_VIEW_MODEL);
            query.Parameters.AddWithValue("tenant_id", NpgsqlDbType.Uuid, tenantId);

            var resp = await _repository.ExecuteStandardCommand(query);
            foreach (var row in resp.rows)
            {

                var users = row["users"];
                foreach (var user in users)
                {
                    ObjectEncryption.DecryptObject(user, CurrentUser.OrgSecret, fieldsForDecrypt, fieldsForDecryptECB);
                    try
                    {
                        user["decr_email"] = AesEncryption.DecryptEcb(user["email"].ToString(), CurrentUser.OrgSecret);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                    }
                }
            }

            return new ServiceResponse { Result = resp };
        }

        public async Task<ServiceResponse> GetVenuesTenantViewModel(VenueViewPayload payload)
        {
            // p_page/p_page_size feed an arithmetic offset calculation in the db function with no internal
            // fallback, so a NULL here breaks pagination (NULL * anything = NULL) instead of defaulting
            var query = new NpgsqlCommand(PredefinedQueryPatterns.VENUES_TENANT_VIEW_MODEL);
            query.Parameters.AddWithValue("p_tenant_id", NpgsqlDbType.Uuid, (object?)payload.tenant_id ?? DBNull.Value);
            query.Parameters.AddWithValue("p_org_code", NpgsqlDbType.Text, (object?)payload.org_code ?? DBNull.Value);
            query.Parameters.AddWithValue("p_venue_id", NpgsqlDbType.Uuid, (object?)payload.venue_id ?? DBNull.Value);
            query.Parameters.AddWithValue("p_page", NpgsqlDbType.Integer, payload.page_size ?? 1);
            query.Parameters.AddWithValue("p_page_size", NpgsqlDbType.Integer, payload.page_size ?? 100);

            var resp = await _repository.ExecuteStandardCommand(query);

            return new ServiceResponse { Result = resp };
        }

        public async Task<ServiceResponse> GetBookList(JObject payload)
        {
            var fieldsForDecrypt = new List<string> { "u_last", "u_first", "u_phone" };
            var fieldsForDecryptECB = new List<string> { "u_email" };

            var query = new NpgsqlCommand(PredefinedQueryPatterns.BOOKING_LIST);
            query.Parameters.AddWithValue("@payload", NpgsqlDbType.Jsonb, JsonConvert.SerializeObject(payload));

            var resp = await _repository.ExecuteStandardCommand(query);
            foreach (var row in resp.rows)
            {
                ObjectEncryption.DecryptObject(row, CurrentUser.OrgSecret, fieldsForDecrypt, fieldsForDecryptECB);
            }

            return new ServiceResponse { Result = resp };
        }

        public async Task<ServiceResponse> GetBookingDetail(Guid bookingId)
        {
            var fieldsForDecrypt = new List<string> { "u_last", "u_first", "u_phone", "u_message", "u_salutation", "u_phone_local" };
            var fieldsForDecryptECB = new List<string> { "u_email" };
            var query = new NpgsqlCommand(PredefinedQueryPatterns.BOOKING_DETAIL);
            query.Parameters.AddWithValue("booking_id", NpgsqlDbType.Uuid, bookingId);

            var resp = await _repository.ExecuteStandardCommand(query);
            foreach (var row in resp.rows)
            {
                ObjectEncryption.DecryptObject(row["booking"], CurrentUser.OrgSecret, fieldsForDecrypt, fieldsForDecryptECB);

                if (IsHelpDesk)
                {
                    var buFields = new List<string> { "create_bu", "modify_bu", "delete_bu" };
                    foreach (var field in buFields)
                    {
                        var value = row["booking"][field]?.ToString();
                        if (string.IsNullOrEmpty(value))
                            continue;

                        try
                        {
                            row["booking"][$"decr_{field}"] = AesEncryption.DecryptEcb(value, CurrentUser.OrgSecret);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.ToString());
                        }
                    }
                }
            }

            return new ServiceResponse { Result = resp };
        }
    }
}
