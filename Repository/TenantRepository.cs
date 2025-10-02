using CommonLibrary.Domain.Entities;
using CommonLibrary.Repository.Interfaces;
using Microsoft.Extensions.Configuration;
using ServicePortal.API.Infrastructure.Repository;
using ServicePortal.Domain.Entities;

namespace CommonLibrary.Repository
{
    public class TenantRepository : GenericRepository<Tenant>, ITenantRepository
    {
        private static (string _table, string _schema) meta = BaseEntity.GetMeta<Tenant>();

        public TenantRepository(IConfiguration config) : base(config)
        {
        }

        public async Task<string> GetOrgCode(Guid id)
        {
            var fields = new Dictionary<string, string>() { { "org_code", "\"\"" } };
            var filters = new Dictionary<string, string>() { { "tenant_id", $"\"{id}\"" } };
            var query = GenerateDoSelectQuery(fields, filters, meta._schema, meta._table);
            var result = await ExecuteDoSelectCommand(query);
            if (result.success && result.rows.Count > 0)
            {
                return result.rows.First().org_code;
            }
            return "";
        }
    }
}
