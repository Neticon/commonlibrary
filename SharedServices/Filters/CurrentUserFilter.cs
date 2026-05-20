using Microsoft.AspNetCore.Mvc.Filters;
using ServicePortal.Application.Interfaces;

namespace CommonLibrary.SharedServices.Filters
{
    public class CurrentUserFilter : IAsyncActionFilter
    {
        private readonly ICurrentUserService _currentUserService;

        public CurrentUserFilter(ICurrentUserService currentUserService)
        {
            _currentUserService = currentUserService;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var email = context.HttpContext.User.FindFirst("custom:postal_code")?.Value;
            var orgCode = context.HttpContext.User.FindFirst("custom:organization_code")?.Value;
            await _currentUserService.GetAsync(email, orgCode);
            await next();
        }
    }
}
