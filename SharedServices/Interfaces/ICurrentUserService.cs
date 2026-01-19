using CommonLibrary.Domain.Entities;
using ServicePortal.Application.Models;

namespace ServicePortal.Application.Interfaces
{
    public interface ICurrentUserService
    {
        Task<CurrentUser> GetAsync(string hashedMail, string orgCode);
        CurrentUser? CurrentUser { get; }
    }
}
