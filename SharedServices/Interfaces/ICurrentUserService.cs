using CommonLibrary.Domain.Entities;
using CommonLibrary.Models;

namespace CommonLibrary.SharedServices.Interfaces
{
    public interface ICurrentUserService
    {
        Task<CurrentUser> GetAsync(string hashedMail, string orgCode);
        CurrentUser? CurrentUser { get; }
    }
}
