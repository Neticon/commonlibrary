using CommonLibrary.Domain.Entities;

namespace CommonLibrary.Repository.Interfaces
{
    public interface ITenantRepository : IGenericEntityRepository<Tenant>
    {
        Task<Tuple<string, string>> GetOrgCodeAndName(Guid id);
        Task<Tenant> GetTenantContext(string orgCode);
        Task<string> GetIntegrationConfig(Guid id);
    }
}
