using CommonLibrary.Domain.Entities;
using CommonLibrary.Helpers;
using CommonLibrary.Repository.Interfaces;
using Microsoft.Extensions.Configuration;
using Npgsql;
using NpgsqlTypes;

namespace CommonLibrary.Repository
{
    public class EventRepository : GenericEntityRepository<Event>, IEventRepository
    {
        public EventRepository(IConfiguration config) : base(config)
        {
        }

        public async Task UpdateEventBody(Guid id, string origin, string body)
        {
            var query = new NpgsqlCommand(PredefinedQueryPatterns.APPEND_EVENT_BODY);
            query.Parameters.AddWithValue("id", NpgsqlDbType.Uuid, id);
            query.Parameters.AddWithValue("origin", origin);
            query.Parameters.AddWithValue("new_body", NpgsqlDbType.Jsonb, body);

            await ExecuteCommandVoid(query);
        }

        public async Task InsertEvent(Event data)
        {
            var query = new NpgsqlCommand(PredefinedQueryPatterns.INSERT_EVENT);
            query.Parameters.AddWithValue("id", NpgsqlDbType.Uuid, data.event_id);
            query.Parameters.AddWithValue("origin", data.origin);
            query.Parameters.AddWithValue("message_type", data.message_type);
            query.Parameters.AddWithValue("body", NpgsqlDbType.Jsonb, DBNull.Value);
            query.Parameters.AddWithValue("reference_entity", data.reference_entity);
            query.Parameters.AddWithValue("reference_id", NpgsqlDbType.Uuid, data.reference_id);
            query.Parameters.AddWithValue("tenant_id", NpgsqlDbType.Uuid, data.tenant_id);

            await ExecuteCommandVoid(query);
        }
    }
}
