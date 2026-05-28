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

        protected AppType AppType => AppConfig.AppType;
        protected bool IsHelpDesk => AppConfig.AppType == AppType.Helpdesk;
    }
}
