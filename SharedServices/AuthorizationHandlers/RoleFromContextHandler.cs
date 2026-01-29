using Microsoft.AspNetCore.Authorization;
using ServicePortal.Application.Interfaces;

public sealed class RoleFromDbHandler
    : AuthorizationHandler<RoleRequirement>
{
    private readonly ICurrentUserService _currentUserService;

    public RoleFromDbHandler(ICurrentUserService currentUserService)
    {
        _currentUserService = currentUserService;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        RoleRequirement requirement)
    {
        var email = context.User.FindFirst("custom:postal_code")?.Value;
        var orgCode = context.User.FindFirst("custom:organization_code")?.Value;
        var user = await _currentUserService.GetAsync(email, orgCode);

        if (user.Role == requirement.Role)
        {
            context.Succeed(requirement);
        }
    }
}
