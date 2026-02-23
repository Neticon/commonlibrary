namespace CommonLibrary.SharedServices.Interfaces
{
    public interface ICounterService
    {
        Task<bool> TriggerCounter(string tenantId, string venueId, string field, bool apiCall = false);
        Task ProcessCounter(string tenantId, string venueId, string path, int code, string method = "");
        Task SyncRedisToDatabase();
    }
}
