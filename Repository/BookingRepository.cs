using CommonLibrary.Domain.Entities;
using CommonLibrary.Helpers;
using CommonLibrary.Integrations.Model;
using CommonLibrary.Models;
using CommonLibrary.Repository.Interfaces;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using ServicePortal.Domain.PSQL;

namespace CommonLibrary.Repository
{
    public class BookingRepository : GenericEntityRepository<Booking>, IBookingRepository
    {
        private static (string _table, string _schema) meta = BaseEntity.GetMeta<Booking>();

        public BookingRepository(IConfiguration config) : base(config)
        {
        }

        public async Task<GraphAPIResponse<Booking>> SaveBooking(Booking data)
        {
            var fieldsDictionary = ObjectConverters.ToPropertyDictionary(data, true);
            var payload = new GraphApiPayload { data = data };
            var query = GenerateDoOperationsQuery(data, meta._schema, meta._table, DoOperationQueryType.insert);
            var queryResult = await ExecuteStandardCommand(query);
            return queryResult;
        }

        public async Task<GraphAPIResponse<Booking>> UpdateBooking(Object model)
        {
            var query = GenerateDoOperationsQuery(model, meta._schema, meta._table, DoOperationQueryType.update);
            var queryResult = await ExecuteStandardCommand(query);
            return queryResult;
        }

        public async Task<BookingViewModel> GetBooking(Guid id)
        {
            var fieldsDictionary = ObjectConverters.ToPropertyDictionary(new BookingViewModel(), true);
            var filters = new Dictionary<string, string> { { "booking_id", $"\"{id}\"" }, { "block_status", "{\"values\": [\"SCHEDULED\",\"RESCHEDULED\"], \"operator\": \"IN\"}" } };
            var query = GenerateDoSelectQuery(fieldsDictionary, filters, meta._schema, meta._table);
            var queryResult = await ExecuteDoSelectCommand(query);
            if (queryResult.success && queryResult.rows != null && queryResult.rows.Count > 0)
            {
                var result = JsonConvert.DeserializeObject<BookingViewModel>(JsonConvert.SerializeObject(queryResult.rows[0]));
                if (!string.IsNullOrEmpty(result.u_reason) && result.u_reason.StartsWith("SRV"))
                    result.service_id = result.u_reason;
                result.u_reason = null;
                return result;
            }
            return null;
        }

        public async Task<string> GetBookingReason(Guid id)
        {
            var fieldName = "u_reason";
            var fieldsDictionary = new Dictionary<string, string> { { fieldName, "\"\"" } };
            var filters = new Dictionary<string, string> { { "booking_id", $"\"{id}\"" }, { "block_status", "{\"values\": [\"SCHEDULED\",\"RESCHEDULED\"], \"operator\": \"IN\"}" } };
            var query = GenerateDoSelectQuery(fieldsDictionary, filters, meta._schema, meta._table);
            var queryResult = await ExecuteDoSelectCommand(query);
            if (queryResult.success && queryResult.rows != null && queryResult.rows.Count > 0)
            {
                var result = JsonConvert.DeserializeObject<dynamic>(JsonConvert.SerializeObject(queryResult.rows[0]));
                return result[fieldName];
            }
            return null;
        }



    }
}
