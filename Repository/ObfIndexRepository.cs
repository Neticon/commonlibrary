using CommonLibrary.Domain.Entities;
using CommonLibrary.Helpers;
using CommonLibrary.Integrations.Model;
using CommonLibrary.Repository.Interfaces;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Npgsql;
using NpgsqlTypes;
using ServicePortal.API.Infrastructure.Repository;

namespace CommonLibrary.Repository
{
    public class ObfIndexRepository : GenericRepository<ObfIndex>, IObfIndexRepository
    {
        public ObfIndexRepository(IConfiguration config) : base(config)
        {
        }

        public async Task<string> InsertBulkIndexes(List<ObfIndexDBModel> models)
        {
            var query = new NpgsqlCommand(PredefinedQueryPatterns.BULK_INSERT_OBF_INDEX);
            query.Parameters.AddWithValue("@payload", NpgsqlTypes.NpgsqlDbType.Jsonb, JsonConvert.SerializeObject(models));
            return await ExecuteCommandString(query);
        }

        public async Task<string> SearchBooking(ObfSearchModel data)
        {
            var query = new NpgsqlCommand(PredefinedQueryPatterns.OBF_INDEX_SEARCH_AND_FETCH);
            query.Parameters.AddWithValue("@payload", NpgsqlDbType.Jsonb, JsonConvert.SerializeObject(data, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));

            return await ExecuteCommandString(query);
        }
    }
}
