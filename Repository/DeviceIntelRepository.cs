using CommonLibrary.Domain.Entities;
using CommonLibrary.Repository.Interfaces;
using Microsoft.Extensions.Configuration;
using ServicePortal.API.Infrastructure.Repository;

namespace CommonLibrary.Repository
{
    public class DeviceIntelRepository<T> :  GenericRepository<T> , IDeviceIntelRepository where T : DeviceIntel
    {
        private static (string _table, string _schema) meta = BaseEntity.GetMeta<DeviceIntel>();
        public DeviceIntelRepository(IConfiguration config) : base(config)
        {
        }

        public async Task CreateDeviceIntel(Guid id, string type, int origin, string apiJsonResponse, string hash)
        {
            await ExecuteCommandVoid(GenerateCreateQuery(id, type, origin, apiJsonResponse, hash));
        }

        public async Task<DeviceIntel> GetDeviceIntel(string hash)
        {
            var query = GenerateDoSelectQuery(null, new Dictionary<string, string>() { { "hash", $"\"{hash}\"" } }, meta._schema, meta._table);
            var queryResult = await ExecuteDoSelectCommand(query);
            if (queryResult.success)
                return queryResult.rows.FirstOrDefault();
            return null;
        }

        public async Task<bool> UpdateDeviceIntel(Guid id, string type, string intel)
        {
            var utcNow = DateTime.UtcNow;
            var ts = DateTime.SpecifyKind(utcNow, DateTimeKind.Unspecified);
            var filter = new Dictionary<string, string>() { { "id", $"\"{id}\"" }, { "type", $"\"{type}\"" } };
            var fileds = new Dictionary<string, string>() { { "intel", $"{intel}" }, { "create_dt", $"\"{DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.ffffff")}\"" } };
            var query = GenerateDoOperationsQuery(fileds, filter, meta._schema, meta._table, Helpers.DoOperationQueryType.update);
            var queryResult = await ExecuteStandardCommand(query);
            return queryResult.success;
        }

        private string GenerateCreateQuery(Guid id, string type, int origin, string apiJsonResponse, string hash)
        {
            return $"SELECT web_app.create_device_intel('{id}', '{type}', {origin}, '{apiJsonResponse}'::jsonb, '{hash}');";
        }

        private string GetDeviceIntelQuery(string hash)
        {
            return $"SELECT * FROM web_app.device_intel WHERE hash = '{hash}';";
        }
    }
}
