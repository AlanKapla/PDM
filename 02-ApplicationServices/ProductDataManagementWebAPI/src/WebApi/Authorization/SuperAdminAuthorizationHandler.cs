using Business.Interfaces.Model;
using Microsoft.AspNetCore.Authorization;

namespace WebApi.Authorization;

public sealed class SuperAdminAuthorizationHandler : AuthorizationHandler<SuperAdminRequirement>
{
    private readonly ICurrentUser currentUser;

    public SuperAdminAuthorizationHandler(ICurrentUser currentUser)
    {
        this.currentUser = currentUser;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        SuperAdminRequirement requirement)
    {
        if (currentUser.IsSuperAdmin)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
