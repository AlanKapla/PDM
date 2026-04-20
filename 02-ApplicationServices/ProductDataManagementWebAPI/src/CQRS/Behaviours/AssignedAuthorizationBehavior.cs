using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CQRS.Behaviours;

public sealed class AssignedAuthorizationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ICurrentUser currentUser;
    private readonly IAccessService accessService;
    private readonly ILogger<AssignedAuthorizationBehavior<TRequest, TResponse>> logger;

    public AssignedAuthorizationBehavior(
        ICurrentUser currentUser,
        IAccessService accessService,
        ILogger<AssignedAuthorizationBehavior<TRequest, TResponse>> logger)
    {
        this.currentUser = currentUser;
        this.accessService = accessService;
        this.logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request is not IAssignedAuthorizableRequest assignedRequest)
        {
            return await next();
        }

        logger.LogDebug(
            "Authorizing assigned request {RequestType} with permission {Permission} for project {ProjectId}",
            typeof(TRequest).Name,
            assignedRequest.PermissionCode,
            assignedRequest.ProjectId);

        var authorized = await accessService.AuthorizeAssignedAsync(
            currentUser,
            assignedRequest.PermissionCode,
            assignedRequest.ProjectId,
            cancellationToken);

        if (!authorized)
        {
            logger.LogWarning(
                "Assigned authorization failed for user {UserId} on request {RequestType} with permission {Permission}",
                currentUser.Id,
                typeof(TRequest).Name,
                assignedRequest.PermissionCode);

            throw new ForbiddenApiException($"You do not have permission to perform this action: {assignedRequest.PermissionCode}");
        }

        return await next();
    }
}
