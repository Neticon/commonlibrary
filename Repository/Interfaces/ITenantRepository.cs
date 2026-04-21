using CommonLibrary.Domain.Entities;

namespace CommonLibrary.Repository.Interfaces
{
    public interface ITenantRepository : IGenericEntityRepository<Tenant>
    {
        Task<Tuple<string, string>> GetOrgCodeAndName(Guid id);
        Task<Guid?> GetTenantId(string orgCode);
        Task<string> GetIntegrationConfig(Guid id);
    }
}
