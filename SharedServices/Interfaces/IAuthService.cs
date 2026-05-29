using CommonLibrary.Models;

namespace CommonLibrary.SharedServices.Interfaces
{
    public interface IAuthService
    {
        Task<Tuple<UserContextModel, string, long>> GetUserContext(string org_code, string hashedMail, string ip);
    }
}
