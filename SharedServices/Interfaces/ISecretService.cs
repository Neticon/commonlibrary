namespace CommonLibrary.SharedServices.Interfaces
{
    public interface ISecretService
    {
        Task<string> GetEncryptionSecret(string orgCode);
        Task<string> GetSecretAsync(string secretName);
        Task CreateSecretAsync(string secretName, string secretValue, string description = "");
    }
}
