using Business.Interfaces.Model;
using Microsoft.AspNetCore.Authorization;

namespace WebApi.Authorization;

public sealed class SuperAdminAuthorizationHandler : AuthorizationHandler<SuperAdminRequirement>
{
    private readonly ICurrentUser currentUser;
    private readonly ILogger<SuperAdminAuthorizationHandler> logger;

    public SuperAdminAuthorizationHandler(
        ICurrentUser currentUser,
        ILogger<SuperAdminAuthorizationHandler> logger)
    {
        this.currentUser = currentUser;
        this.logger = logger;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        SuperAdminRequirement requirement)
    {
        if (currentUser.IsAuthenticated && currentUser.IsSuperAdmin)
        {
            logger.LogDebug("SuperAdmin authorization succeeded for user {UserId}", currentUser.Id);
            context.Succeed(requirement);
        }
        else
        {
            logger.LogWarning(
                "SuperAdmin authorization failed — user {UserId} is not a SuperAdmin",
                currentUser.IsAuthenticated ? currentUser.Id : Guid.Empty);
            context.Fail();
        }

        return Task.CompletedTask;
    }
}
