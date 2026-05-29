using CommonLibrary.SharedServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using ServicePortal.Application.Interfaces;
using WebApp.API.Controllers.Helper;

public sealed class RoleFromDbHandler
    : AuthorizationHandler<RoleRequirement>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private const string REQUEST_ID_HEADER = "X-Request-ID";

    public RoleFromDbHandler(ICurrentUserService currentUserService, IHttpContextAccessor httpContextAccessor)
    {
        _currentUserService = currentUserService;
        _httpContextAccessor = httpContextAccessor;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        RoleRequirement requirement)
    {
        var email = context.User.FindFirst("custom:postal_code")?.Value;

        string orgCode;
        if (AppConfig.AppType == AppType.Helpdesk)
        {
            _httpContextAccessor.HttpContext.Request.Headers.TryGetValue(REQUEST_ID_HEADER, out var headerOrgCode);
            orgCode = headerOrgCode;
        }
        else
        {
            orgCode = context.User.FindFirst("custom:organization_code")?.Value;
        }

        var user = await _currentUserService.GetAsync(email, orgCode);

        var userRoleInt = CommonHelperFunctions.GetRoleInt(user.Role);
        var requiredRoleInt = CommonHelperFunctions.GetRoleInt(requirement.Role);

        if (userRoleInt >= requiredRoleInt)
        {
            context.Succeed(requirement);
        }
    }
}
