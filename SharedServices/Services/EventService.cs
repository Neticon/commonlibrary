using CommonLibrary.Domain.Entities;
using CommonLibrary.Helpers;
using CommonLibrary.Models;
using CommonLibrary.Repository.Interfaces;
using CommonLibrary.SharedServices.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Npgsql;
using NpgsqlTypes;

namespace CommonLibrary.SharedServices.Services
{
    public class EventService : IEventService
    {
        private readonly IGenericEntityRepository<Event> _eventRepository;

        public EventService(IGenericEntityRepository<Event> eventRepository)
        {
            _eventRepository = eventRepository;
        }

        public async Task<ServiceResponse> GetData(object payload)
        {
            var json = JsonConvert.DeserializeObject<JObject>(payload.ToString());
            var resp = await _eventRepository.GetData(json);
            return new ServiceResponse { Result = resp };
        }

        public async Task<ServiceResponse> GetDataHD(object payload)
        {
            var query = new NpgsqlCommand(PredefinedQueryPatterns.HD_EVENT_LIST);
            query.Parameters.AddWithValue("@p_payload", NpgsqlDbType.Jsonb, JsonConvert.SerializeObject(payload));

            var resp = await _eventRepository.ExecuteStandardCommand(query);

            return new ServiceResponse { Result = resp };
        }
    }
}
