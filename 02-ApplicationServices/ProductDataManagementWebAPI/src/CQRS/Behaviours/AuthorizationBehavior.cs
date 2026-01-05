using Business.Implementation.Services;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using CQRS.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CQRS.Behaviours;

public class AuthorizationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ICurrentUser currentUser;
    private readonly AccessService accessService;
    private readonly ILogger<AuthorizationBehavior<TRequest, TResponse>> logger;

    public AuthorizationBehavior(
        ICurrentUser currentUser,
        AccessService accessService,
        ILogger<AuthorizationBehavior<TRequest, TResponse>> logger)
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
        if (request is not IAuthorizableRequest authorizableRequest)
        {
            // Request doesn't implement IAuthorizableRequest, skip authorization
            return await next();
        }

        var permissionCode = authorizableRequest.PermissionCode;
        var resource = authorizableRequest.GetResource();
        var resourceScope = authorizableRequest.GetResourceScope();

        logger.LogDebug(
            "Authorizing request {RequestType} with permission {Permission} for tenant {TenantId}, resourceScope {ResourceScope}",
            typeof(TRequest).Name,
            permissionCode,
            resource.TenantId,
            resourceScope);

        var authorized = await accessService.AuthorizeAsync(
            currentUser,
            permissionCode,
            resource,
            resourceScope,
            cancellationToken);

        if (!authorized)
        {
            logger.LogWarning(
                "Authorization failed for user {UserId} on request {RequestType} with permission {Permission}",
                currentUser.Id,
                typeof(TRequest).Name,
                permissionCode);

            throw new ForbiddenApiException($"You do not have permission to perform this action: {permissionCode}");
        }

        logger.LogDebug(
            "Authorization succeeded for user {UserId} on request {RequestType}",
            currentUser.Id,
            typeof(TRequest).Name);

        return await next();
    }
}
