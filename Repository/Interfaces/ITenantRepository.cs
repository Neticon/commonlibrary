namespace CommonLibrary.Repository.Interfaces
{
    public interface ITenantRepository
    {
        Task<Tuple<string, string>> GetOrgCodeAndName(Guid id);
        Task<Guid?> GetTenantId(string orgCode);
    }
}
