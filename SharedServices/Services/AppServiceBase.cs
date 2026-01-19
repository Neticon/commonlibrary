using ServicePortal.Application.Interfaces;
using ServicePortal.Application.Models;

namespace CommonLibrary.SharedServices.Services
{
    public abstract class AppServiceBase
    {
        protected ICurrentUserService CurrentUserService { get; }

        protected AppServiceBase(ICurrentUserService currentUserService)
        {
            CurrentUserService = currentUserService;
        }

        protected CurrentUser? CurrentUser
        {
            get => CurrentUserService.CurrentUser;
        }
    }
}
