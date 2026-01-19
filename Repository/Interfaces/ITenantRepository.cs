namespace CommonLibrary.Repository.Interfaces
{
    public interface ITenantRepository
    {
        Task<string> GetOrgCode(Guid id);
        Task<Guid?> GetTenantId(string orgCode);
    }
}
