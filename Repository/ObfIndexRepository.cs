using CommonLibrary.Domain.Entities;
using CommonLibrary.Helpers;
using CommonLibrary.Integrations.Model;
using CommonLibrary.Repository.Interfaces;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Npgsql;

namespace CommonLibrary.Repository
{
    public class ObfIndexRepository : GenericEntityRepository<ObfIndex>, IObfIndexRepository
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
    }
}
