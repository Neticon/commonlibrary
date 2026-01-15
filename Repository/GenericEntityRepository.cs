using CommonLibrary.Domain.Entities;
using CommonLibrary.Domain.PSQL;
using CommonLibrary.Helpers;
using CommonLibrary.Models;
using CommonLibrary.Repository.Interfaces;
using CommonLibrary.SharedServices.Interfaces;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using ServicePortal.API.Infrastructure.Repository;
using ServicePortal.Domain.PSQL;
using System.Net.Sockets;

namespace CommonLibrary.Repository
{
    public class GenericEntityRepository<T> : GenericRepository<T>, IGenericEntityRepository<T> where T : BaseEntity, new()
    {
        private static (string _table, string _schema) meta = BaseEntity.GetMeta<T>();

        public GenericEntityRepository(IConfiguration config) : base(config) { }

        public async Task<GraphAPIResponse<T>> SaveEntity(T data, string secret = "", bool returnError = false)
        {
            var encryptPaths = EncryptionMetadataHelper.GetEncryptedPropertyPaths(typeof(T));
            if (encryptPaths.Count > 0 && string.IsNullOrEmpty(secret))
                ThrowEncryptionException(encryptPaths);
            ObjectEncryption.EncryptObject(data, "key", EncryptionMetadataHelper.GetEncryptedPropertyPaths(typeof(T)));
            var payload = new GraphApiPayload { data = data };
            var query = GenerateDoOperationsQuery(payload, data._schema, data._table, DoOperationQueryType.insert);
            var queryResult = await ExecuteStandardCommand(query, returnError);
            return queryResult;
        }

        public async Task<GraphAPIResponse<T>> UpdateEntity(Object model, string secret = "", bool returnError = false)
        {
            var encryptPaths = EncryptionMetadataHelper.GetEncryptedPropertyPaths(typeof(T));
            if (encryptPaths.Count > 0 && string.IsNullOrEmpty(secret))
                ThrowEncryptionException(encryptPaths);
            ObjectEncryption.EncryptObject(model, secret, encryptPaths);
            var query = GenerateDoOperationsQuery(model, meta._schema, meta._table, DoOperationQueryType.update);
            var queryResult = await ExecuteStandardCommand(query, returnError);
            return queryResult;
        }

        public async Task<DoSelectOperationResponse<JObject>> GetData(Object model, string secret = "")
        {
            var query = GenerateDoSelectQuery(model, meta._schema, meta._table);
            var queryResult = await ExecuteDoSelectCommandObject(query);
            if (queryResult.rows != null && queryResult.rows.Count > 0)
            {
                var encryptPaths = EncryptionMetadataHelper.GetEncryptedPropertyPaths(typeof(T));
                if (encryptPaths.Count > 0 && string.IsNullOrEmpty(secret))
                    ThrowEncryptionException(encryptPaths);
                foreach (var row in queryResult.rows)
                {
                    ObjectEncryption.DecryptObject(row, secret, encryptPaths);
                }
            }
            return queryResult;
        }

        private void ThrowEncryptionException(List<string> encryptPaths)
        {
            throw new Exception($"Encryption secret is empty for object = {typeof(T).Name} and fields {string.Join(',', encryptPaths)}");
        }
    }
}
