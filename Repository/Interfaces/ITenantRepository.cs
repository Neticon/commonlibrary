namespace CommonLibrary.Repository.Interfaces
{
    public interface ITenantRepository
    {
        Task<string> GetOrgCode(Guid id);
    }
}
