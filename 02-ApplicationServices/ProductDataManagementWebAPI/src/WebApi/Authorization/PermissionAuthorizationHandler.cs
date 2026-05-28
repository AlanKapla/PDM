using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;

namespace WebApi.Authorization;

/// <summary>
/// Authorization handler that validates permissions based on their scope (Global, Tenant, Project, Resource)
/// </summary>
public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly ICurrentUser currentUser;
    private readonly IAccessService accessService;
    private readonly IHttpContextAccessor httpContextAccessor;
    private readonly ILogger<PermissionAuthorizationHandler> logger;

    public PermissionAuthorizationHandler(
        ICurrentUser currentUser,
        IAccessService accessService,
        IHttpContextAccessor httpContextAccessor,
        ILogger<PermissionAuthorizationHandler> logger)
    {
        this.currentUser = currentUser;
        this.accessService = accessService;
        this.httpContextAccessor = httpContextAccessor;
        this.logger = logger;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            logger.LogWarning("HttpContext is null in PermissionAuthorizationHandler");
            context.Fail();
            return;
        }

        var routeData = httpContext.GetRouteData();
        
        Guid tenantId = Guid.Empty;
        Guid? projectId = null;
        ResourceScope? resourceScope = null;

        // Try to extract ResourceScope from route
        if (routeData.Values.TryGetValue("scope", out var scopeValue) && 
            Enum.TryParse<ResourceScope>(scopeValue?.ToString(), true, out var parsedScope))
        {
            resourceScope = parsedScope;
            logger.LogDebug("Extracted ResourceScope {Scope} from route", parsedScope);
        }

        // Extract required identifiers based on permission scope
        switch (requirement.Scope)
        {
            case PermissionScope.Global:
                // Global permissions don't require any route parameters
                logger.LogDebug("Processing global permission {Permission}", requirement.PermissionCode);
                break;
                
            case PermissionScope.Tenant:
                // Tenant permissions require tenantId
                if (!TryGetGuid(routeData, "tenantId", out var tId))
                {
                    logger.LogWarning(
                        "TenantId not found or invalid in route for tenant-scoped permission {Permission}",
                        requirement.PermissionCode);
                    context.Fail();
                    return;
                }
                tenantId = tId;
                break;
                
            case PermissionScope.Project:
                // Project permissions require BOTH tenantId and projectId
                if (!TryGetGuid(routeData, "tenantId", out var tIdForProject))
                {
                    logger.LogWarning(
                        "TenantId not found or invalid in route for project-scoped permission {Permission}",
                        requirement.PermissionCode);
                    context.Fail();
                    return;
                }
                tenantId = tIdForProject;
                
                if (!TryGetGuid(routeData, "projectId", out var pId))
                {
                    logger.LogWarning(
                        "ProjectId not found or invalid in route for project-scoped permission {Permission}",
                        requirement.PermissionCode);
                    context.Fail();
                    return;
                }
                projectId = pId;
                break;
        }

        var resource = new ResourceRef(tenantId, projectId);

        try
        {
            var authorized = await accessService.AuthorizeAsync(
                currentUser,
                requirement.PermissionCode,
                resource,
                resourceScope,
                httpContext.RequestAborted);

            if (authorized)
            {
                context.Succeed(requirement);
                logger.LogDebug(
                    "Authorization succeeded for user {UserId} with permission {Permission} (scope: {Scope}, resourceScope: {ResourceScope}) on tenant {TenantId}, project {ProjectId}",
                    currentUser.Id,
                    requirement.PermissionCode,
                    requirement.Scope,
                    resourceScope,
                    tenantId,
                    projectId);
            }
            else
            {
                logger.LogWarning(
                    "Authorization failed for user {UserId} with permission {Permission} (scope: {Scope}, resourceScope: {ResourceScope}) on tenant {TenantId}, project {ProjectId}",
                    currentUser.Id,
                    requirement.PermissionCode,
                    requirement.Scope,
                    resourceScope,
                    tenantId,
                    projectId);
                context.Fail();
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, 
                "Error during authorization for user {UserId} with permission {Permission} (scope: {Scope})",
                currentUser.Id,
                requirement.PermissionCode,
                requirement.Scope);
            context.Fail();
        }
    }

    /// <summary>
    /// Tries to extract a Guid from route data
    /// </summary>
    private static bool TryGetGuid(RouteData routeData, string key, out Guid value)
    {
        value = default;
        return routeData.Values.TryGetValue(key, out var obj) &&
               Guid.TryParse(obj?.ToString(), out value);
    }

}
