using CommonLibrary.Domain.Entities;
using CommonLibrary.Domain.PSQL;
using CommonLibrary.Helpers;
using CommonLibrary.Repository.Interfaces;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ServicePortal.API.Infrastructure.Repository;
using ServicePortal.Domain.PSQL;

namespace CommonLibrary.Repository
{
    public class GenericEntityRepository<T> : GenericRepository<T>, IGenericEntityRepository<T> where T : BaseEntity, new()
    {
        private static (string _table, string _schema) meta = BaseEntity.GetMeta<T>();

        public GenericEntityRepository(IConfiguration config) : base(config)
        {
        }

        public async Task<GraphAPIResponse<T>> SaveEntity(T data)
        {
            ObjectEncryption.EncryptObject(data, "key", EncryptionMetadataHelper.GetEncryptedPropertyPaths(typeof(T)));
            var fieldsDictionary = ObjectConverters.ToPropertyDictionary(data, true);
            var json = JsonConvert.SerializeObject(data);
            var query = GenerateDoOperationsQuery(fieldsDictionary, new Dictionary<string, string>(), data._schema, data._table, DoOperationQueryType.insert);
            var queryResult = await ExecuteStandardCommand(query);
            return queryResult;
        }

        public async Task<GraphAPIResponse<T>> UpdateEntity(Object model)
        {
            ObjectEncryption.EncryptObject(model, "key", EncryptionMetadataHelper.GetEncryptedPropertyPaths(typeof(T)));
            var query = GenerateDoOperationsQuery(model, meta._schema, meta._table, DoOperationQueryType.update);
            var queryResult = await ExecuteStandardCommand(query);
            return queryResult;
        }

        public async Task<DoSelectOperationResponse<JObject>> GetData(Object model)
        {
            var query = GenerateDoSelectQuery(model, meta._schema, meta._table);
            var queryResult = await ExecuteDoSelectCommandObject(query);
            if (queryResult.rows != null && queryResult.rows.Count > 0)
            {
                foreach (var row in queryResult.rows)
                {
                    ObjectEncryption.DecryptObject(row, "key", EncryptionMetadataHelper.GetEncryptedPropertyPaths(typeof(T)));
                }
            }
            return queryResult;
        }
    }
}
