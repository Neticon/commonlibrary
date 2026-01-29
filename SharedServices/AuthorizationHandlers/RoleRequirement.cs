using Microsoft.AspNetCore.Authorization;

public sealed class RoleRequirement : IAuthorizationRequirement
{
    public string Role { get; }

    public RoleRequirement(string role)
    {
        Role = role;
    }
}

