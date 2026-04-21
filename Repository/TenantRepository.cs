using CommonLibrary.Domain.Entities;
using CommonLibrary.Models;
using CommonLibrary.Repository.Interfaces;
using Microsoft.Extensions.Configuration;
using ServicePortal.API.Infrastructure.Repository;

namespace CommonLibrary.Repository
{
    public class TenantRepository : GenericEntityRepository<Tenant>, ITenantRepository
    {
        private static (string _table, string _schema) meta = BaseEntity.GetMeta<Tenant>();

        public TenantRepository(IConfiguration config) : base(config)
        {
        }

        public async Task<string> GetIntegrationConfig(Guid id)
        {
            var intg_video = (await GetDataTyped(new GraphApiPayload { data = new Tenant { intg_video = "" }, filters = new Tenant { tenant_id = id } })).rows.First().intg_video;
            return intg_video;
        }

        public async Task<Tuple<string,string>> GetOrgCodeAndName(Guid id)
        {
            var fields = new Dictionary<string, string>() { { "org_code", "\"\"" }, { "org_name", "\"\"" } };
            var filters = new Dictionary<string, string>() { { "tenant_id", $"\"{id}\"" } };
            var query = GenerateDoSelectQuery(fields, filters, meta._schema, meta._table);
            var result = await ExecuteDoSelectCommand(query);
            if (result.success && result.rows.Count > 0 && result.rows.First() != null)
            {
                var tenant = result.rows.First();
                return new Tuple<string, string>(tenant.org_code, tenant.org_name);
            }
            return null;
        }

        public async Task<Guid?> GetTenantId(string orgCode)
        {
            var fields = new Dictionary<string, string>() { { "tenant_id", "\"\"" } };
            var filters = new Dictionary<string, string>() { { "org_code", $"\"{orgCode}\"" } };
            var query = GenerateDoSelectQuery(fields, filters, meta._schema, meta._table);
            var result = await ExecuteDoSelectCommand(query);
            if (result.success && result.rows.Count > 0)
            {
                return result.rows.First().tenant_id;
            }
            return null;
        }
    }
}
