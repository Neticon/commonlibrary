namespace CommonLibrary.SharedServices.Interfaces
{
    public interface ISecretService
    {
        Task<string> GetSecret(string orgCode);
    }
}
