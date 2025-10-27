using CommonLibrary.Domain.Entities;
using CommonLibrary.Repository.Interfaces;
using Microsoft.Extensions.Configuration;
using ServicePortal.API.Infrastructure.Repository;

namespace CommonLibrary.Repository
{
    public class VenueRepository : GenericRepository<Venue>, IVenueRepository
    {
        private static (string _table, string _schema) meta = BaseEntity.GetMeta<Venue>();

        public VenueRepository(IConfiguration config) : base(config)
        {
        }

        public async Task<string> GetVenueTimezone(Guid id)
        {
            var fields = new Dictionary<string, string>() { { "time_zone", "\"\"" } };
            var filters = new Dictionary<string, string>() { { "venue_id", $"\"{id}\"" } };
            var query = GenerateDoSelectQuery(fields, filters, meta._schema, meta._table);
            var result = await ExecuteDoSelectCommand(query);
            if (result.success && result.rows.Count > 0)
            {
                return result.rows.First().time_zone;
            }
            return "";
        }
    }
}
