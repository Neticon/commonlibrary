using CommonLibrary.Domain.Entities;
using CommonLibrary.Helpers;
using CommonLibrary.Integrations.Model;
using CommonLibrary.Repository.Interfaces;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using ServicePortal.API.Infrastructure.Repository;
using ServicePortal.Domain.PSQL;

namespace CommonLibrary.Repository
{
    public class BookingRepository : GenericRepository<Booking>, IBookingRepository 
    {
        private static (string _table, string _schema) meta = BaseEntity.GetMeta<Booking>();

        public BookingRepository(IConfiguration config) : base(config)
        {
        }

        public async Task<DoOperationResponse<Booking>> SaveBooking(Booking data)
        {
            var fieldsDictionary = ObjectConverters.ToPropertyDictionary(data, true);
            var query = GenerateDoOperationsQuery(fieldsDictionary, new Dictionary<string, string>(), meta._schema, meta._table, DoOperationQueryType.insert);
            var queryResult = await ExecuteDoOperationsCommand(query);
            return queryResult;
        }

        public async Task<DoOperationResponse<Booking>> UpdateBooking(Object model)
        {
            var query = GenerateDoOperationsQuery(model, meta._schema, meta._table, DoOperationQueryType.update);
            var queryResult = await ExecuteDoOperationsCommand(query);
            return queryResult;
        }

        public async Task<BookingViewModel> GetBooking(Guid id)
        {
            var fieldsDictionary = ObjectConverters.ToPropertyDictionary(new BookingViewModel(), false);
            var filters = new Dictionary<string, string> { { "booking_id", $"\"{id}\"" }, { "block_status", "{\"values\": [\"SCHEDULED\",\"RESCHEDULED\"], \"operator\": \"IN\"}" } };
            var query = GenerateDoSelectQuery(fieldsDictionary, filters, meta._schema, meta._table);
            var queryResult = await ExecuteDoSelectCommand(query);
            if(queryResult.success && queryResult.rows != null && queryResult.rows.Count > 0)
            {
                return JsonConvert.DeserializeObject<BookingViewModel>(JsonConvert.SerializeObject(queryResult.rows[0]));
            }
            return null;
        }



    }
}
