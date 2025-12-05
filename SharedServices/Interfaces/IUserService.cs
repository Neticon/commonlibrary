using CommonLibrary.Models.API;
using ServicePortal.Application.Models;

namespace CommonLibrary.SharedServices.Interfaces
{
    public interface IUserService
    {
        Task CreateNewUser(CreateUserModel model);
        Task UpdateUser(UpdateUserModel model);
        Task DeleteUser(DeleteUserModel model);
        Task<object> GetUsers(string model);
    }
}
