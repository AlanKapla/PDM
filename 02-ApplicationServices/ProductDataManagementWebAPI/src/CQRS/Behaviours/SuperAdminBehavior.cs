using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CQRS.Behaviours;

public sealed class SuperAdminBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ICurrentUser currentUser;
    private readonly ILogger<SuperAdminBehavior<TRequest, TResponse>> logger;

    public SuperAdminBehavior(
        ICurrentUser currentUser,
        ILogger<SuperAdminBehavior<TRequest, TResponse>> logger)
    {
        this.currentUser = currentUser;
        this.logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request is not ISuperAdminRequest)
        {
            return await next();
        }

        if (!currentUser.IsAuthenticated || !currentUser.IsSuperAdmin)
        {
            logger.LogWarning(
                "SuperAdmin access denied for request {RequestType} — user {UserId} is not a SuperAdmin",
                typeof(TRequest).Name,
                currentUser.IsAuthenticated ? currentUser.Id : Guid.Empty);

            throw new UnauthorizedApiException();
        }

        logger.LogDebug(
            "SuperAdmin access granted for request {RequestType} by user {UserId}",
            typeof(TRequest).Name,
            currentUser.Id);

        return await next();
    }
}
